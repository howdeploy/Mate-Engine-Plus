using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Action = System.Action;
using Application = UnityEngine.Application;

public class TrayMenuEntry
{
    public string Label;
    public Action Callback;
    public bool IsToggle;
    public bool InitialState;

    public TrayMenuEntry(string label, Action callback = null, bool isToggle = false, bool initialState = false)
    {
        Label = label;
        Callback = callback;
        IsToggle = isToggle;
        InitialState = initialState;
    }
}

public class TrayIndicator : MonoBehaviour
{
    #region API
    public enum AppIndicatorCategory
    {
        ApplicationStatus = 0,
        Communications = 1,
        SystemServices = 2,
        Other = 3
    }

    public enum AppIndicatorStatus
    {
        Passive = 0,   // Hidden
        Active = 1,    // Visible with normal icon
        Attention = 2  // Visible with attention icon (e.g., blinking)
    }
    
    private const string LibraryName = "libayatana-appindicator3.so.1";

    [DllImport(LibraryName)]
    private static extern IntPtr app_indicator_new(string id, string icon_name, AppIndicatorCategory category);

    [DllImport(LibraryName)]
    private static extern void app_indicator_set_status(IntPtr indicator, AppIndicatorStatus status);

    [DllImport(LibraryName)]
    private static extern void app_indicator_set_icon(IntPtr indicator, string icon_name);

    [DllImport(LibraryName)]
    private static extern void app_indicator_set_attention_icon(IntPtr indicator, string attention_icon_name);

    [DllImport(LibraryName)]
    private static extern void app_indicator_set_menu(IntPtr indicator, IntPtr menu);
    
    [DllImport(LibraryName)]
    private static extern void app_indicator_set_icon_full(IntPtr indicator, string icon_name, string icon_desc);

    private IntPtr indicatorHandle;
    private Gtk.Menu menu;
    private List<GCHandle> delegateHandles = new();
    
    #endregion

    public static TrayIndicator Instance;
    
    Dictionary<IntPtr, Action> MenuActions = new();

    public Func<List<TrayMenuEntry>> OnBuildMenu;

    private void OnEnable()
    {
        Instance = this;
    }
    
    private void Update()
    {
        if (indicatorHandle != IntPtr.Zero)
        {
            while (GLib.MainContext.Pending())
            {
                GLib.MainContext.Iteration();
            }
        }
    }
    
    public void InitializeTrayIcon(string iconName)
    {
#if  UNITY_EDITOR
        return;
#endif
        // Create the indicator with a unique ID, normal icon name (use a theme icon like "applications-system"), and category
        indicatorHandle =
            app_indicator_new(iconName, "applications-system", AppIndicatorCategory.ApplicationStatus);

        if (indicatorHandle == IntPtr.Zero)
        {
            Debug.LogError("Failed to create AppIndicator");
            return;
        }

        // Set to active status with normal icon
        app_indicator_set_status(indicatorHandle, AppIndicatorStatus.Active);
#if UNITY_EDITOR
        app_indicator_set_icon_full(indicatorHandle,
            Application.dataPath + "/MATE ENGINE - Icons/DevICON_70x70.png", Application.productName);
#else
        app_indicator_set_icon_full(indicatorHandle, Application.dataPath + "/Resources/UnityPlayer.png", Application.productName);
#endif
    }

    public void AddMenuItem(List<TrayMenuEntry> menuEntries)
    {
        #if UNITY_EDITOR
        return;
        #endif
        CreateMenu(menuEntries);
    }

    private void RefreshMenu()
    {
        CleanupMenu();
        if (OnBuildMenu != null)
        {
            CreateMenu(OnBuildMenu());
        }
    }

    private void CreateMenu(List<TrayMenuEntry> menuEntries)
    {
        menu = new Gtk.Menu();
        if (menuEntries != null)
        {
            foreach (var entry in menuEntries)
            {
                if (entry.Label == "Separator")
                {
                    var separator = new Gtk.SeparatorMenuItem();
                    menu.Append(separator);
                    separator.Show();
                }
                else
                {
                    Gtk.MenuItem menuItem = null;
                    Gtk.CheckMenuItem checkMenuItem = null;
                    bool isToggle = entry.IsToggle;

                    if (isToggle)
                    {
                        // Create toggle (check) menu item
                        checkMenuItem = new Gtk.CheckMenuItem(entry.Label);
                        checkMenuItem.Active = entry.InitialState;
                        menu.Append(checkMenuItem);
                        checkMenuItem.Show();
                    }
                    else
                    {
                        // Regular menu item
                        menuItem = new Gtk.MenuItem(entry.Label);
                        menu.Append(menuItem);
                        menuItem.Show();
                    }
                    
                    if (entry.Callback != null)
                    {
                        MenuActions.Add(isToggle ? checkMenuItem.Handle : menuItem.Handle, entry.Callback);
                    }

                    if (menuItem != null)
                        menuItem.Activated += (_, _) => { OnMenuItemClicked(menuItem.Handle); };

                    if (checkMenuItem != null)
                        checkMenuItem.Activated += (_, _) => { OnMenuItemClicked(checkMenuItem.Handle); };
                }
            }
        }
        
        app_indicator_set_menu(indicatorHandle, menu.Handle);
    }

    private void OnMenuItemClicked(IntPtr menuItem)
    {
        if (MenuActions.ContainsKey(menuItem))
        {
            MenuActions[menuItem].Invoke();
        }

        RefreshMenu();
    }
    
    void OnApplicationQuit()
    {
        if (indicatorHandle != IntPtr.Zero)
        {
            app_indicator_set_status(indicatorHandle, AppIndicatorStatus.Passive);
            indicatorHandle = IntPtr.Zero;
        }
        CleanupMenu();
    }

    void OnDestroy()
    {
        CleanupMenu();
    }

    private void CleanupMenu()
    {
        MenuActions.Clear();
        if (menu != null)
        {
            menu.Dispose();
            menu = null;
        }
        foreach (var handle in delegateHandles)
        {
            if (handle.IsAllocated)
                handle.Free();
        }
        delegateHandles.Clear();
    }
}