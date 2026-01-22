cd ParticleLeniaResarch01/Assets
powershell -NoExit -command "Get-ChildItem -Recurse -Filter *.cs | Get-Content | Measure-Object -Line -Character"
