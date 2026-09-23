using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace OneBoardInlineTranslate.Services;

internal sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ToolStripMenuItem _pauseItem;

    internal TrayIconService(
        Action openSettings,
        Action togglePause,
        Action showAbout,
        Action exit)
    {
        var menu = new Forms.ContextMenuStrip();
        var title = new Forms.ToolStripMenuItem("OneBoard Inline Translate") { Enabled = false };
        var status = new Forms.ToolStripMenuItem("Status: Running") { Enabled = false, Name = "status" };
        var settings = new Forms.ToolStripMenuItem("Open Settings", null, (_, _) => openSettings());
        _pauseItem = new Forms.ToolStripMenuItem("Pause Translation", null, (_, _) => togglePause());
        var about = new Forms.ToolStripMenuItem("About", null, (_, _) => showAbout());
        var exitItem = new Forms.ToolStripMenuItem("Exit", null, (_, _) => exit());
        menu.Items.AddRange([
            title,
            status,
            new Forms.ToolStripSeparator(),
            settings,
            _pauseItem,
            new Forms.ToolStripSeparator(),
            about,
            exitItem
        ]);

        _icon = new Forms.NotifyIcon
        {
            Icon = LoadApplicationIcon(),
            Text = "OneBoard Inline Translate - Running",
            ContextMenuStrip = menu,
            Visible = true
        };
        _icon.DoubleClick += (_, _) => openSettings();
    }

    internal void SetPaused(bool paused)
    {
        var status = _icon.ContextMenuStrip?.Items.Find("status", searchAllChildren: false).FirstOrDefault();
        if (status is not null)
        {
            status.Text = paused ? "Status: Paused" : "Status: Running";
        }

        _pauseItem.Text = paused ? "Resume Translation" : "Pause Translation";
        _icon.Text = paused
            ? "OneBoard Inline Translate - Paused"
            : "OneBoard Inline Translate - Running";
    }

    internal void ShowNotice(
        string title,
        string message,
        Forms.ToolTipIcon icon = Forms.ToolTipIcon.Info)
    {
        _icon.BalloonTipTitle = title;
        _icon.BalloonTipText = message;
        _icon.BalloonTipIcon = icon;
        _icon.ShowBalloonTip(4_000);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }

    private static Drawing.Icon LoadApplicationIcon()
    {
        var executable = Environment.ProcessPath;
        return !string.IsNullOrWhiteSpace(executable)
            ? Drawing.Icon.ExtractAssociatedIcon(executable) ?? Drawing.SystemIcons.Application
            : Drawing.SystemIcons.Application;
    }
}
