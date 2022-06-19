@ECHO OFF

nuget install packages.config -OutputDirectory packages/

msbuild /p:SolutionDir=%CD% /p:Configuration=Debug
msbuild /p:SolutionDir=%CD% /p:Configuration=Release