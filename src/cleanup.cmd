@echo off
for /d %%d in (*) do rd %%d\bin /s /q
for /d %%d in (*) do rd %%d\obj /s /q
