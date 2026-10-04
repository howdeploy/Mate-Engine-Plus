namespace APIs.Hyprland
{
    public static class HyprlandEventNames
    {
        // Title of a window changed
        public const string WindowTitleV2 = "windowtitlev2";
        public const string WindowTitle = "windowtitle";

        // The active window changed
        public const string ActiveWindow = "activewindow";
        public const string ActiveWindowV2 = "activewindowv2";

        // A window was moved to another workspace
        public const string MoveWindowToWorkspace = "movewindow";
        public const string MoveWindowToWorkspaceV2 = "movewindowv2";

        // The active workspace changed
        public const string MoveToWorkspace = "workspace";
        public const string MoveToWorkspaceV2 = "workspacev2";

        // A workspace was moved to another monitor
        public const string MoveToWorkspaceToMonitor = "moveworkspace";
        public const string MoveToWorkspaceToMonitorV2 = "moveworkspacev2";

        // A workspace was destroyed
        public const string DestroyWorkspace = "destroyworkspace";
        public const string DestroyWorkspaceV2 = "destroyworkspacev2";

        // A workspace was created
        public const string CreateWorkspace = "createworkspace";
        public const string CreateyWorkspaceV2 = "createworkspacev2";

        // The special workspace was created/changed/destroyed
        public const string ActivateSpecialWorkspace = "activespecial";
        public const string ActivateSpecialWorkspaceV2 = "activespecialv2";

        // A Window was created
        public const string OpenWindow = "openwindow";
        // A window was closed
        public const string CloseWindow = "closewindow";
        // A windows was killed
        public const string KillWindow = "kill";

        // the pin state of window changed
        public const string PinState = "pin";

        // The current monitor changed
        public const string FocusMonitor = "focusedmon";
        public const string FocusMonitorV2 = "focusedmonv2";

        // A monitor was removed
        public const string MonitorRemoved = "monitorremoved";
        public const string MonitorRemovedV2 = "monitorremovedv2";

        // A monitor was added
        public const string MonitorAdded = "monitoradded";
        public const string MonitorAddedV2 = "monitoraddedv2";

        // A surface was addded to a layer
        public const string LayerAdded = "openlayer";
        // A surface was removed from a layer
        public const string LayerRemoved = "closelayer";

        // A windows was moved into a windowgroup
        public const string MoveWindowIntoGroup = "moveintogroup";
        // A windows was moved out of a windowgroup
        public const string MoveWindowOutOfGroup = "moveoutofgroup";
    }
}