# XUnityProfiler
A dotnet program to automatically generate BeginSample/EndSample at the Start/End of every function

Use --help or -h to see help messages
Lastest help message ↓↓↓
Arguments:
        -p, --Path The path to the file(.cs) or a folder (default: "")
        -?, -h, --help  ShowHelp

Example:
File(Path: D:/temp.cs):
```
public class CancelKeyHolding : KeyHolding {
    private void OnEnable() {
        inputActions.OnCancel += StartHold;
        inputActions.OnCancelUp += EndHold;
    }

    private void OnDisable() {
        inputActions.OnCancel -= StartHold;
        inputActions.OnCancelUp -= EndHold;

        EndHold();
    }

    public string GetInfo() {
        if (Enabled)
        {
            return "Enabled";
        }
        else
        {
            return "Disabled";
        }
    }
}
```

Run:
.\XUnityProfiler.exe -p D:/temp.cs
or
.\XUnityProfiler.exe --Path D:/temp.cs

converts the file to ↓↓↓
New File(Path: D:/temp.cs):
```
public class CancelKeyHolding : KeyHolding {
    private void OnEnable() {
        UnityEngine.Profiling.Profiler.BeginSample("CancelKeyHolding.cs OnEnable");
        inputActions.OnCancel += StartHold;
        inputActions.OnCancelUp += EndHold;
        UnityEngine.Profiling.Profiler.EndSample();
    }

    private void OnDisable() {
        UnityEngine.Profiling.Profiler.BeginSample("CancelKeyHolding.cs OnDisable");
        inputActions.OnCancel -= StartHold;
        inputActions.OnCancelUp -= EndHold;

        EndHold();
        UnityEngine.Profiling.Profiler.EndSample();
    }

    public string GetInfo() {
        UnityEngine.Profiling.Profiler.BeginSample("CancelKeyHolding.cs GetInfo");
        if (Enabled)
        {
            UnityEngine.Profiling.Profiler.EndSample();
            return "Enabled";
        }
        else
        {
            UnityEngine.Profiling.Profiler.EndSample();
            return "Disabled";
        }
        UnityEngine.Profiling.Profiler.EndSample();
    }
}
```

Works on
- Normal Function (with or without return)
- Generic Function

Dosen't work on
- IEnumerble Functions(Ones have 'yield return')
- async Functions
- Getter/Setters (Because I am lazy)
- Multi-thread codes
- Inline if/for blocks ↓↓↓ (Please don't write codes like this at all time !!!)
```
public string GetInfo() {
    if (Enabled) return "Enabled";
    if (!Enabled) return "Disabled";
}
```