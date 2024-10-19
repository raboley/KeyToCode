using System.Collections.Generic;

namespace KeyScripter;

public class Config
{
    public Dictionary<string, string> KeyActions { get; set; }
    public string? LastSelectedProcessName { get; set; }
    public string? LastSavedFilePath { get; set; }
    public bool AutomaticallySelectLastProcess { get; set; } = true;
    public bool AutomaticallyCopyOutputToClipboard { get; set; } = true;
    public bool AutomaticallyOpenLastSavedFile { get; set; } = false;
    public bool LoopPlayback { get; set; } = false;
}