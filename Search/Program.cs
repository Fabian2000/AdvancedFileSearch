// All comments in English
using System;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

public partial class MainForm : Form
{
    private FolderBrowserDialog folderBrowserDialog;
    private Button btnSelectFolder;
    private TextBox txtPattern;
    private Panel panelPattern;
    private TextBox txtFolder;
    private Panel panelFolder;
    private TextBox txtExclusions;
    private Panel panelExclusions;
    private ListView resultsListView;
    private CheckBox chkSearchContent;
    private CheckBox chkSearchFilename;
    private Button btnSearch;
    private Label lblStatus;
    private Label lblPattern;
    private Label lblFolder;
    private Label lblExclusions;
    private Button btnToggleTheme;
    private bool isDarkMode = true;
    private readonly ConcurrentBag<SearchResult> results = new ConcurrentBag<SearchResult>();
    private bool isSearching = false;

    private static readonly Color DarkBackground = Color.FromArgb(32, 32, 32);
    private static readonly Color DarkForeground = Color.FromArgb(240, 240, 240);
    private static readonly Color DarkControl = Color.FromArgb(45, 45, 48);

    private static readonly Color LightBackground = SystemColors.Control;
    private static readonly Color LightForeground = SystemColors.ControlText;
    private static readonly Color LightControl = SystemColors.Window; // white-ish

    public MainForm()
    {
        InitializeComponent();
        SetupListView();
        ApplyTheme();
    }

    private void SetupListView()
    {
        resultsListView.View = View.Details;
        resultsListView.FullRowSelect = true;
        resultsListView.GridLines = false;
        resultsListView.Columns.Add("File", 300);
        resultsListView.Columns.Add("Line", 50);
        resultsListView.Columns.Add("Content", 400);
    }

    private void InitializeComponent()
    {
        folderBrowserDialog = new FolderBrowserDialog();
        btnSelectFolder = new Button();
        txtPattern = new TextBox();
        txtFolder = new TextBox();
        txtExclusions = new TextBox();
        resultsListView = new ListView();
        chkSearchContent = new CheckBox();
        chkSearchFilename = new CheckBox();
        btnSearch = new Button();
        lblStatus = new Label();
        lblPattern = new Label();
        lblFolder = new Label();
        lblExclusions = new Label();
        btnToggleTheme = new Button();

        // Label for exclude examples
        Label lblExcludeHelp = new Label
        {
            Text = "Examples: *.dll; *.exe; node_modules",
            ForeColor = SystemColors.GrayText,
            Location = new Point(120, 115),
            AutoSize = true
        };

        // Form settings
        Text = "Advanced File Search";
        Size = new Size(800, 600);
        MinimumSize = new Size(800, 600);

        // Labels
        lblFolder.Text = "Folder:";
        lblFolder.AutoSize = true;
        lblFolder.Location = new Point(12, 15);

        lblPattern.Text = "Pattern:";
        lblPattern.AutoSize = true;
        lblPattern.Location = new Point(12, 44);

        lblExclusions.Text = "Exclude:";
        lblExclusions.AutoSize = true;
        lblExclusions.Location = new Point(12, 95);

        // Panel + TextBox for Folder
        panelFolder = new Panel
        {
            Location = new Point(120, 12),
            Size = new Size(542, 20),
            BorderStyle = BorderStyle.FixedSingle
        };
        txtFolder.BorderStyle = BorderStyle.None;
        txtFolder.Location = new Point(1, 1);
        txtFolder.Size = new Size(panelFolder.Width - 2, panelFolder.Height - 2);
        panelFolder.Controls.Add(txtFolder);

        // Browse button
        btnSelectFolder.Location = new Point(670, 10);
        btnSelectFolder.Size = new Size(100, 23);
        btnSelectFolder.Text = "Browse";
        btnSelectFolder.FlatStyle = FlatStyle.Flat;
        btnSelectFolder.Click += btnSelectFolder_Click;

        // Panel + TextBox for Pattern
        panelPattern = new Panel
        {
            Location = new Point(120, 40),
            Size = new Size(542, 20),
            BorderStyle = BorderStyle.FixedSingle
        };
        txtPattern.BorderStyle = BorderStyle.None;
        txtPattern.Location = new Point(1, 1);
        txtPattern.Size = new Size(panelPattern.Width - 2, panelPattern.Height - 2);
        panelPattern.Controls.Add(txtPattern);

        // Panel + TextBox for Exclusions
        panelExclusions = new Panel
        {
            Location = new Point(120, 92),
            Size = new Size(542, 20),
            BorderStyle = BorderStyle.FixedSingle
        };
        txtExclusions.BorderStyle = BorderStyle.None;
        txtExclusions.Location = new Point(1, 1);
        txtExclusions.Size = new Size(panelExclusions.Width - 2, panelExclusions.Height - 2);
        txtExclusions.PlaceholderText = "node_modules; .git; *.dll; ...";
        txtExclusions.TabIndex = 4;
        panelExclusions.Controls.Add(txtExclusions);

        // Theme toggle button
        btnToggleTheme.Location = new Point(670, 70);
        btnToggleTheme.Size = new Size(100, 23);
        btnToggleTheme.Text = "🌓 Theme";
        btnToggleTheme.FlatStyle = FlatStyle.Flat;
        btnToggleTheme.Click += btnToggleTheme_Click;

        // Checkboxes
        chkSearchFilename.Location = new Point(120, 70);
        chkSearchFilename.Text = "Include Filename";
        chkSearchFilename.Checked = true;
        chkSearchFilename.AutoSize = true;

        chkSearchContent.Location = new Point(250, 70);
        chkSearchContent.Text = "Include File Content";
        chkSearchContent.Checked = true;
        chkSearchContent.AutoSize = true;

        // Search button
        btnSearch.Location = new Point(670, 40);
        btnSearch.Size = new Size(100, 23);
        btnSearch.Text = "Search";
        btnSearch.FlatStyle = FlatStyle.Flat;
        btnSearch.Click += btnSearch_Click;

        // Status label - lowered
        lblStatus.Location = new Point(120, 130);
        lblStatus.AutoSize = true;

        // Results list view
        resultsListView.Location = new Point(12, 160);
        resultsListView.Size = new Size(760, 390);
        resultsListView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        resultsListView.DoubleClick += resultsListView_DoubleClick;

        // Add everything
        Controls.AddRange(new Control[]
        {
            lblFolder,
            lblPattern,
            lblExclusions,
            panelFolder,
            btnSelectFolder,
            panelPattern,
            panelExclusions,
            chkSearchFilename,
            chkSearchContent,
            btnSearch,
            btnToggleTheme,
            lblStatus,
            resultsListView,
            lblExcludeHelp
        });
    }

    private void ApplyTheme()
    {
        // Determine colors for current theme
        Color bgColor = isDarkMode ? DarkBackground : LightBackground;
        Color fgColor = isDarkMode ? DarkForeground : LightForeground;
        Color controlColor = isDarkMode ? DarkControl : LightControl;

        BackColor = bgColor;
        ForeColor = fgColor;

        // Panels + TextBoxes
        // Folder
        panelFolder.BackColor = controlColor;
        txtFolder.BackColor = controlColor;
        txtFolder.ForeColor = fgColor;
        txtFolder.Font = new Font("Segoe UI", 9F);

        // Pattern
        panelPattern.BackColor = controlColor;
        txtPattern.BackColor = controlColor;
        txtPattern.ForeColor = fgColor;
        txtPattern.Font = new Font("Segoe UI", 9F);

        // Exclusions
        panelExclusions.BackColor = controlColor;
        txtExclusions.BackColor = controlColor;
        txtExclusions.ForeColor = fgColor;
        txtExclusions.Font = new Font("Segoe UI", 9F);

        // Other controls
        foreach (Control ctrl in Controls)
        {
            switch (ctrl)
            {
                case Button btn:
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                    btn.BackColor = controlColor;
                    btn.ForeColor = fgColor;
                    btn.Cursor = Cursors.Hand;
                    btn.Font = new Font("Segoe UI", 9F);
                    break;

                case CheckBox chk:
                    chk.FlatStyle = FlatStyle.Standard;
                    chk.BackColor = bgColor;
                    chk.ForeColor = fgColor;
                    chk.Font = new Font("Segoe UI", 9F);
                    break;

                case Label lbl:
                    lbl.BackColor = bgColor;
                    lbl.ForeColor = fgColor;
                    lbl.Font = new Font("Segoe UI", 9F);
                    break;

                case ListView lv:
                    lv.BackColor = controlColor;
                    lv.ForeColor = fgColor;
                    lv.BorderStyle = BorderStyle.None;
                    lv.Font = new Font("Segoe UI", 9F);
                    break;
            }
        }
    }

    private void btnToggleTheme_Click(object sender, EventArgs e)
    {
        isDarkMode = !isDarkMode;
        ApplyTheme();
    }

    private void btnSelectFolder_Click(object sender, EventArgs e)
    {
        if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
        {
            txtFolder.Text = folderBrowserDialog.SelectedPath;
        }
    }

    private async void btnSearch_Click(object sender, EventArgs e)
    {
        if (isSearching)
        {
            isSearching = false;
            btnSearch.Text = "Search";
            return;
        }

        if (string.IsNullOrWhiteSpace(txtFolder.Text) || string.IsNullOrWhiteSpace(txtPattern.Text))
        {
            MessageBox.Show("Please select a folder and enter a search pattern.");
            return;
        }

        if (!chkSearchFilename.Checked && !chkSearchContent.Checked)
        {
            MessageBox.Show("Please select at least one search option.");
            return;
        }

        isSearching = true;
        btnSearch.Text = "Stop";
        resultsListView.Items.Clear();
        results.Clear();
        lblStatus.Text = "Searching...";

        try
        {
            await SearchFilesAsync(txtFolder.Text, new Regex(txtPattern.Text, RegexOptions.IgnoreCase));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during search: {ex.Message}");
        }
        finally
        {
            isSearching = false;
            btnSearch.Text = "Search";
            lblStatus.Text = $"Found {results.Count} results";
        }
    }

    private async Task SearchFilesAsync(string folder, Regex pattern)
    {
        await Task.Run(() =>
        {
            try
            {
                var exclusions = txtExclusions.Text
                    .Split(';')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories);

                Parallel.ForEach(files, file =>
                {
                    if (!isSearching) return;

                    // Check exclusions
                    bool isExcluded = exclusions.Any(exclusion =>
                    {
                        if (exclusion.StartsWith("*."))
                        {
                            return file.EndsWith(exclusion.Substring(1), StringComparison.OrdinalIgnoreCase);
                        }
                        return file.Contains(exclusion, StringComparison.OrdinalIgnoreCase);
                    });

                    if (isExcluded) return;

                    try
                    {
                        // Filename match
                        if (chkSearchFilename.Checked && pattern.IsMatch(Path.GetFileName(file)))
                        {
                            AddResult(new SearchResult(file, 0, "Filename match"));
                        }

                        // Content match
                        if (chkSearchContent.Checked)
                        {
                            SearchInFile(file, pattern);
                        }
                    }
                    catch
                    {
                        // skip inaccessible
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing folder: {ex.Message}");
            }
        });
    }

    private void SearchInFile(string file, Regex pattern)
    {
        try
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (!isSearching) return;
                if (pattern.IsMatch(lines[i]))
                {
                    AddResult(new SearchResult(file, i + 1, lines[i].Trim()));
                }
            }
        }
        catch
        {
            // skip unreadable
        }
    }

    private void AddResult(SearchResult result)
    {
        results.Add(result);
        BeginInvoke(new Action(() =>
        {
            var item = new ListViewItem(new[]
            {
                result.FilePath,
                result.LineNumber.ToString(),
                result.Content
            });
            resultsListView.Items.Add(item);
        }));
    }

    private void resultsListView_DoubleClick(object sender, EventArgs e)
    {
        if (resultsListView.SelectedItems.Count > 0)
        {
            var filePath = resultsListView.SelectedItems[0].SubItems[0].Text;
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true,
                    Verb = "openas"
                };
                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}");
            }
        }
    }
}

public class SearchResult
{
    public string FilePath { get; }
    public int LineNumber { get; }
    public string Content { get; }

    public SearchResult(string filePath, int lineNumber, string content)
    {
        FilePath = filePath;
        LineNumber = lineNumber;
        Content = content;
    }
}

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
