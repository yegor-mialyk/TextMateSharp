using System.Diagnostics;
using TextMateSharp.Grammars;

namespace TextMateSharp.Model;

public class TMModel : ITMModel
{
    private const int MAX_LEN_TO_TOKENIZE = 10000;
    private readonly Queue<int> _invalidLines = new();
    private readonly IModelLines _lines;
    private readonly object _lock = new();
    private readonly ManualResetEvent _resetEvent = new(false);
    private readonly List<IModelTokensChangedListener> listeners;
    private IGrammar _grammar;
    private TokenizerThread _thread;
    private Tokenizer _tokenizer;

    public TMModel(IModelLines lines)
    {
        listeners = new();
        _lines = lines;
        ((AbstractLineList) lines).SetModel(this);
    }

    public bool IsStopped => _thread == null || _thread.IsStopped;

    public IGrammar GetGrammar()
    {
        return _grammar;
    }

    public void SetGrammar(IGrammar grammar)
    {
        if (!Equals(grammar, _grammar))
        {
            Stop();

            _grammar = grammar;
            _lines.ForEach(line => line.ResetTokenizationState());

            if (grammar != null)
            {
                _tokenizer = new(grammar);
                _lines.Get(0).State = _tokenizer.GetInitialState();
                Start();
                InvalidateLine(0);
            }
            else
            {
                Emit(new(new Range(0, _lines.GetNumberOfLines() - 1), this));
            }
        }
    }

    public void AddModelTokensChangedListener(IModelTokensChangedListener listener)
    {
        if (_grammar != null)
            Start();

        lock (listeners)
        {
            if (!listeners.Contains(listener))
                listeners.Add(listener);
        }
    }

    public void RemoveModelTokensChangedListener(IModelTokensChangedListener listener)
    {
        lock (listeners)
        {
            listeners.Remove(listener);
            if (listeners.Count == 0)
                // no need to keep tokenizing if no-one cares
                Stop();
        }
    }

    public void Dispose()
    {
        lock (listeners)
        {
            listeners.Clear();
        }

        Stop();
        GetLines().Dispose();
    }

    public void ForceTokenization(int lineIndex)
    {
        ForceTokenization(lineIndex, lineIndex);
    }

    public List<TMToken> GetLineTokens(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex > _lines.GetNumberOfLines() - 1)
            return null;

        return _lines.Get(lineIndex).Tokens;
    }

    private void Stop()
    {
        if (_thread == null)
            return;

        _thread.Stop();
        _resetEvent.Set();
        _thread = null;
    }

    private void Start()
    {
        if (_thread == null || _thread.IsStopped)
            _thread = new("TMModelThread", this);

        if (_thread.IsStopped)
            _thread.Run();
    }

    private void BuildEventWithCallback(Action<ModelTokensChangedEventBuilder> callback)
    {
        if (_thread == null || _thread.IsStopped)
            return;

        var eventBuilder = new ModelTokensChangedEventBuilder(this);

        callback(eventBuilder);

        var e = eventBuilder.Build();
        if (e != null)
            Emit(e);
    }

    private void Emit(ModelTokensChangedEvent e)
    {
        lock (listeners)
        {
            foreach (var listener in listeners)
                try
                {
                    listener.ModelTokensChanged(e);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
        }
    }

    public void ForceTokenization(int startLineIndex, int endLineIndex)
    {
        if (_grammar == null)
            return;

        var tokenizerThread = _thread;

        if (tokenizerThread == null || tokenizerThread.IsStopped)
            return;

        BuildEventWithCallback(eventBuilder =>
            tokenizerThread.UpdateTokensInRange(eventBuilder, startLineIndex, endLineIndex)
        );
    }

    public bool IsLineInvalid(int lineIndex)
    {
        return _lines.Get(lineIndex).IsInvalid;
    }

    public void InvalidateLine(int lineIndex)
    {
        var line = _lines.Get(lineIndex);
        if (line == null)
            return;

        line.IsInvalid = true;

        lock (_lock)
        {
            _invalidLines.Enqueue(lineIndex);
            _resetEvent.Set();
        }
    }

    public void InvalidateLineRange(int iniLineIndex, int endLineIndex)
    {
        lock (_lock)
        {
            for (var i = iniLineIndex; i <= endLineIndex; i++)
            {
                _lines.Get(i).IsInvalid = true;
                _invalidLines.Enqueue(i);
            }

            _resetEvent.Set();
        }
    }

    public IModelLines GetLines()
    {
        return _lines;
    }

    private class TokenizerThread
    {
        private readonly TMModel model;
        public volatile bool IsStopped;
        private TMState lastState;

        private string name;

        public TokenizerThread(string name, TMModel model)
        {
            this.name = name;
            this.model = model;
            IsStopped = true;
        }

        public void Run()
        {
            IsStopped = false;

            ThreadPool.QueueUserWorkItem(ThreadWorker);
        }

        public void Stop()
        {
            IsStopped = true;
        }

        private void ThreadWorker(object state)
        {
            if (IsStopped)
                return;

            do
            {
                var toProcess = -1;

                if (model._grammar.IsCompiling)
                {
                    model._resetEvent.Reset();
                    model._resetEvent.WaitOne();
                    continue;
                }

                lock (model._lock)
                {
                    if (model._invalidLines.Count > 0)
                        toProcess = model._invalidLines.Dequeue();
                }

                if (toProcess == -1)
                {
                    model._resetEvent.Reset();
                    model._resetEvent.WaitOne();
                    continue;
                }

                var modelLine = model._lines.Get(toProcess);

                if (modelLine == null || !modelLine.IsInvalid)
                    continue;

                try
                {
                    RevalidateTokens(toProcess, null);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);

                    if (toProcess < model._lines.GetNumberOfLines())
                        model.InvalidateLine(toProcess);
                }
            } while (!IsStopped && model._thread != null);
        }

        private void RevalidateTokens(int startLine, int? toLineIndexOrNull)
        {
            if (model._tokenizer == null)
                return;

            model.BuildEventWithCallback(eventBuilder =>
            {
                var toLineIndex = toLineIndexOrNull ?? 0;
                if (toLineIndexOrNull == null || toLineIndex >= model._lines.GetNumberOfLines())
                    toLineIndex = model._lines.GetNumberOfLines() - 1;

                long tokenizedChars = 0;
                long currentCharsToTokenize = 0;
                long MAX_ALLOWED_TIME = 5;
                long currentEstimatedTimeToTokenize = 0;
                long elapsedTime;
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                // Tokenize at most 1000 lines. Estimate the tokenization speed per
                // character and stop when:
                // - MAX_ALLOWED_TIME is reached
                // - tokenizing the next line would go above MAX_ALLOWED_TIME

                var lineIndex = startLine;
                while (lineIndex <= toLineIndex && lineIndex < model.GetLines().GetNumberOfLines())
                {
                    elapsedTime = stopwatch.ElapsedMilliseconds;
                    if (elapsedTime > MAX_ALLOWED_TIME)
                    {
                        // Stop if MAX_ALLOWED_TIME is reached
                        model.InvalidateLine(lineIndex);
                        return;
                    }

                    // Compute how many characters will be tokenized for this line
                    try
                    {
                        currentCharsToTokenize = model._lines.GetLineLength(lineIndex);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                    if (tokenizedChars > 0)
                    {
                        // If we have enough history, estimate how long tokenizing this line would take
                        currentEstimatedTimeToTokenize =
                            (long) ((double) elapsedTime / tokenizedChars) * currentCharsToTokenize;
                        if (elapsedTime + currentEstimatedTimeToTokenize > MAX_ALLOWED_TIME)
                        {
                            // Tokenizing this line will go above MAX_ALLOWED_TIME
                            model.InvalidateLine(lineIndex);
                            return;
                        }
                    }

                    lineIndex = UpdateTokensInRange(eventBuilder, lineIndex, lineIndex) + 1;
                    tokenizedChars += currentCharsToTokenize;
                }
            });
        }

        public int UpdateTokensInRange(ModelTokensChangedEventBuilder eventBuilder, int startIndex,
            int endLineIndex)
        {
            var stopLineTokenizationAfter = TimeSpan.FromMilliseconds(3000);
            var nextInvalidLineIndex = startIndex;
            var lineIndex = startIndex;
            while (lineIndex <= endLineIndex && lineIndex < model._lines.GetNumberOfLines())
            {
                if (model._grammar != null && model._grammar.IsCompiling)
                {
                    lineIndex++;
                    continue;
                }

                var endStateIndex = lineIndex + 1;
                LineTokens? r;
                string? text;
                var modeLine = model._lines.Get(lineIndex);
                try
                {
                    text = model._lines.GetLineText(lineIndex);
                    if (text == null)
                        continue;
                    // Tokenize only the first X characters
                    r = model._tokenizer.Tokenize(text, modeLine?.State, 0, MAX_LEN_TO_TOKENIZE,
                        stopLineTokenizationAfter);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    lineIndex++;
                    continue;
                }

                if (r != null && r.Tokens != null && r.Tokens.Count != 0)
                    // Cannot have a stop offset before the last token
                    r.ActualStopOffset = Math.Max(r.ActualStopOffset, r.Tokens[r.Tokens.Count - 1].StartIndex + 1);

                if (r != null && r.ActualStopOffset < text.Length)
                {
                    // Treat the rest of the line (if above limit) as one default token
                    r.Tokens.Add(new(r.ActualStopOffset, new()));
                    // Use as end state the starting state
                    r.EndState = modeLine.State;
                }

                if (r == null)
                    r = new(new() { new(0, new()) }, text.Length,
                        modeLine.State);

                modeLine.Tokens = r.Tokens;
                eventBuilder.registerChangedTokens(lineIndex + 1);
                modeLine.IsInvalid = false;

                if (endStateIndex < model._lines.GetNumberOfLines())
                {
                    var endStateLine = model._lines.Get(endStateIndex);
                    if (endStateLine.State != null && r.EndState.Equals(endStateLine.State))
                    {
                        // The end state of this line remains the same
                        nextInvalidLineIndex = lineIndex + 1;
                        while (nextInvalidLineIndex < model._lines.GetNumberOfLines())
                        {
                            var isLastLine = nextInvalidLineIndex + 1 >= model._lines.GetNumberOfLines();
                            if (model._lines.Get(nextInvalidLineIndex).IsInvalid ||
                                (!isLastLine && model._lines.Get(nextInvalidLineIndex + 1).State == null) ||
                                (isLastLine && lastState == null))
                                break;

                            nextInvalidLineIndex++;
                        }

                        lineIndex = nextInvalidLineIndex;
                    }
                    else
                    {
                        endStateLine.State = r.EndState;
                        lineIndex++;
                    }
                }
                else
                {
                    lastState = r.EndState;
                    lineIndex++;
                }
            }

            return nextInvalidLineIndex;
        }
    }
}
