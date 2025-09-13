﻿using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Tests.Resources;

namespace TextMateSharp.Tests.Internal.Grammars.Reader
{
    class GrammarReaderTests
    {
        [Test]
        public void Read_Csharp_Grammar_Should_Not_Throw_Any_Exception()
        {
            using (Stream ms = ResourceReader.OpenStream("csharp.tmLanguage.json"))
            using (StreamReader reader = new StreamReader(ms))
            {
                IRawGrammar grammar = GrammarReader.ReadGrammarSync(reader);
                Assert.Equals("source.cs", grammar.GetScopeName());
            }
        }

        [Test]
        public void Read_Csharp_Grammar_Should_Be_Well_Formed()
        {
            using (Stream ms = GenerateStreamFromString(_json))
            using (StreamReader reader = new StreamReader(ms))
            {
                IRawGrammar grammar = GrammarReader.ReadGrammarSync(reader);

                Assert.IsNotNull(grammar);
                Assert.Equals("C#", grammar.GetName());
                Assert.Equals("source.cs", grammar.GetScopeName());

                ICollection<IRawRule> patterns = grammar.GetPatterns();

                Assert.Equals(2, patterns.Count);

                int i = 0;
                foreach (IRawRule pattern in patterns)
                {
                    switch (i)
                    {
                        case 0:
                            Assert.Equals("#preprocessor", pattern.GetInclude());
                            break;

                        case 1:
                            Assert.Equals("#comment", pattern.GetInclude());
                            break;
                    }

                    i++;
                }

                IRawRepository repository = grammar.GetRepository();

                IRawRule rule = repository.GetProp("extern-alias-directive");
                Assert.Equals("[beginregex]+", rule.GetBegin());
                Assert.Equals("[endregexp]+", rule.GetEnd());

                IRawCaptures captures = rule.GetBeginCaptures();

                IRawRule capture1 = captures.GetCapture("1");
                IRawRule capture2 = captures.GetCapture("2");

                Assert.Equals(
                    "keyword.other.extern.cs",
                    capture1.GetName());
                Assert.Equals(
                    "keyword.other.alias.cs",
                    capture2.GetName());
            }
        }

        static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        static string _json =
            @"{
                'name': 'C#',
                'scopeName': 'source.cs',
                'patterns': [
                    {
                        'include': '#preprocessor'
                    },
                    {
                        'include': '#comment'
                    }
                ],
                'repository': {
                      'extern-alias-directive': {
                          'begin': '[beginregex]+',
                          'beginCaptures': {
                              '1': {
                                  'name': 'keyword.other.extern.cs'
                              },
                              '2': {
                                  'name': 'keyword.other.alias.cs'
                              }
                            },
                            'end': '[endregexp]+'
                      }
                }
            }".Replace("'", "\"");
    }
}
