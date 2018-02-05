// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SessionHistoryViewer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using DiffPlex;
    using DiffPlex.DiffBuilder;
    using DiffPlex.DiffBuilder.Model;
    using DiffPlex.Model;
    using Microsoft.Xbox.Services.DevTools.Authentication;
    using Newtonsoft.Json;

    using SessionHistoryViewer.DataContracts;

    public partial class Form1 : Form
    {
        private const string LocalTimeColumnHeader = "Timestamp (Local)";
        private const string UtcTimeColumnHeader = "Timestamp (UTC)";
        private const string FeedbackEmailAddress = "multiplayer@microsoft.com";
        private const string SessionHistoryFileFilter = "Session History files (*.hist)|*.hist";
        private const string QueryFailure = "Query Failure";
        private const string SaveInstructions = "To save a session history offline, right click on an item below.";
        private const int NumLookbackDays = 7;
        private const int QueryResultColumnCount = 6;
        private const int DocumentMetadataColumnCount = 7;

        private const int SideMargin = 18;
        private const int VertMargin = 4;

        private SessionHistorySnapshotCache snapshotCache = new SessionHistorySnapshotCache();
        private static bool vLeftScrollInProgress;
        private static bool vRightScrollInProgress;

        private UserSettings userSettings = new UserSettings();

        private Stack<QueryBundle> queryStack = new Stack<QueryBundle>();
        private bool showingOfflineSession;
        private bool queryCancelled;
        private bool isLV1SortOrderDescending = true;
        private bool isLV2SortOrderDescending = true;

        private DevAccount signedInuser = null;

        public Form1()
        {
            this.InitializeComponent();
            
            this.listView1.View = View.Details;
            this.listView1.GridLines = true;
            this.listView1.FullRowSelect = true;

            string lv1ColumnWidths = this.userSettings.ListView1ColumnWidths;
            string[] c1Widths = lv1ColumnWidths.Split(new char[] { ',' });

            this.displayDateTimesAsUTCToolStripMenuItem.Checked = this.userSettings.ShowLocalTime;

            this.listView1.AddColumn("SessionName", c1Widths, 0, 231);
            this.listView1.AddColumn("Branch", c1Widths, 1, 231);
            this.listView1.AddColumn("Changes", c1Widths, 2, 55);            
            this.listView1.AddColumn(this.userSettings.ShowLocalTime ? LocalTimeColumnHeader : UtcTimeColumnHeader, c1Widths, 3, 140);
            this.listView1.AddColumn("Expired?", c1Widths, 4, 65);
            this.listView1.AddColumn("ActivityId", c1Widths, 5, 250);
            
            this.listView2.View = View.Details;
            this.listView2.GridLines = true;
            this.listView2.FullRowSelect = true;

            string lv2ColumnWidths = this.userSettings.ListView2ColumnWidths;
            string[] c2Widths = lv2ColumnWidths.Split(new char[] { ',' });

            this.listView2.AddColumn("Change", c2Widths, 0, 50);
            this.listView2.AddColumn("Modified By", c2Widths, 1, 120);
            this.listView2.AddColumn(this.userSettings.ShowLocalTime ? LocalTimeColumnHeader : UtcTimeColumnHeader, c2Widths, 2, 120);
            this.listView2.AddColumn("TitleId", c2Widths, 3, 80);
            this.listView2.AddColumn("ServiceId", c2Widths, 4, 100);
            this.listView2.AddColumn("CorrelationId", c2Widths, 5, 250);
            this.listView2.AddColumn("Changes", c2Widths, 6, 450);

            this.dateTimePicker1.MinDate = DateTime.Today.AddDays(-NumLookbackDays);
            this.dateTimePicker1.MaxDate = DateTime.Today;

            this.dateTimePicker2.MinDate = DateTime.Today.AddDays(-NumLookbackDays);
            this.dateTimePicker2.MaxDate = DateTime.Today.AddDays(1);

            this.dateTimePicker1.Value = DateTime.Today.AddDays(-NumLookbackDays);
            this.dateTimePicker2.Value = DateTime.Today;

            this.tbSandbox.Text = this.userSettings.Sandbox;
            this.tbScid.Text = this.userSettings.Scid;
            this.tbTemplateName.Text = this.userSettings.TemplateName;
            this.tbQueryKey.Text = this.userSettings.QueryKey;
            this.cmbQueryType.SelectedIndex = this.userSettings.QueryType;

            this.UpdateAccountPanel();
        }

        private async Task<Tuple<HttpStatusCode, string>> QueryForHistoricalSessionDocuments(string eToken, QueryBundle queryBundle, string continuationToken)
        {
            if (continuationToken == null)
            {
                this.InitViews();
                this.listView1.Items.Clear();
            }

            Tuple<System.Net.HttpStatusCode, string> response = null;

            switch (this.cmbQueryType.SelectedIndex)
            {
                case (int)QueryBy.SessionName:
                    {
                        response = await SessionHistory.QuerySessionHistoryBranches(
                            queryBundle.Scid,
                            queryBundle.TemplateName,
                            queryBundle.QueryKey,
                            eToken);
                    }

                    break;

                case (int)QueryBy.GamerTag:
                    {
                        response = await SessionHistory.QuerySessionHistoryByGamertagAsync(
                            queryBundle.Scid,
                            queryBundle.TemplateName,
                            queryBundle.QueryKey,
                            queryBundle.QueryStartDate,
                            queryBundle.QueryEndDate,
                            continuationToken,
                            eToken);
                    }

                    break;

                case (int)QueryBy.GamerXuid:
                    {
                        response = await SessionHistory.QuerySessionHistoryByXuidAsync(
                            queryBundle.Scid,
                            queryBundle.TemplateName,
                            long.Parse(queryBundle.QueryKey),
                            queryBundle.QueryStartDate,
                            queryBundle.QueryEndDate,
                            continuationToken,
                            eToken);
                    }

                    break;

                case (int)QueryBy.CorrelationId:
                    {
                        response = await SessionHistory.QuerySessionHistoryByCorrelationIdAsync(
                            queryBundle.Scid,
                            queryBundle.TemplateName,
                            this.tbQueryKey.Text,
                            eToken);
                    }

                    break;
            }

            if (response.Item1 != HttpStatusCode.OK)
            {
                return response;
            }

            SessionHistoryQueryResponse queryResponse = new SessionHistoryQueryResponse(response.Item2);
            
            foreach (var item in queryResponse.Results)
            {
                string[] arr = new string[QueryResultColumnCount] 
                { 
                    item.SessionName,
                    item.Branch,
                    item.Changes.ToString(),
                    this.displayDateTimesAsUTCToolStripMenuItem.Checked ? item.LastModified.ToString() : item.LastModified.ToLocalTime().ToString(),
                    item.IsExpired.ToString(),
                    item.ActivityId
                };

                ListViewItem lvi = new ListViewItem(arr);
                this.listView1.Items.Add(lvi);
            }

            this.lblDocCount.Text = string.Format("{0} session{1}", this.listView1.Items.Count, (this.listView1.Items.Count != 1) ? "s" : string.Empty);
            this.lblDocCount.Text += "      [" + SaveInstructions + "]";

            if (this.queryCancelled)
            {
                return new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, null); // ignore continuation token if user cancelled the query
            }
            else
            {
                return new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, queryResponse.ContinuationToken);
            }
        }

        private void InitViews()
        {
            this.listView2.Items.Clear();
            this.rtbSnapshotLeft.Text = string.Empty;
            this.rtbSnapshotRight.Text = string.Empty;
            this.lblChangeLeft.Text = string.Empty;
            this.lblChangeRight.Text = string.Empty;
        }

        private bool ValidateControl(Control control)
        {
            if (string.IsNullOrEmpty(control.Text))
            {
                Console.Beep();
                control.BackColor = Color.FromKnownColor(KnownColor.Aquamarine);
                control.Focus();
                return false;
            }

            control.BackColor = Color.FromKnownColor(KnownColor.Window);
            return true;
        }

        private bool InputsAreValid()
        {
            if (!this.ValidateControl(this.tbSandbox)) return false;

            Guid guid;
            if (!Guid.TryParseExact(this.tbScid.Text, "D", out guid))
            {
                Console.Beep();
                this.tbScid.BackColor = Color.FromKnownColor(KnownColor.Aquamarine);
                this.tbScid.Focus();
                return false;
            }
            
            this.tbScid.BackColor = Color.FromKnownColor(KnownColor.Window);

            if (!this.ValidateControl(this.tbTemplateName)) return false;
            if (!this.ValidateControl(this.tbQueryKey)) return false;

            if (this.cmbQueryType.SelectedIndex == (int)QueryBy.GamerXuid)
            {
                long xuid;
                if (!long.TryParse(this.tbQueryKey.Text, out xuid))
                {
                    Console.Beep();
                    this.tbQueryKey.BackColor = Color.FromKnownColor(KnownColor.Aquamarine);
                    this.tbQueryKey.Focus();
                    return false;
                }
            }

            return true;
        }

        private async void ListView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count == 0)
            {
                return;
            }

            if (!this.showingOfflineSession)
            {
                this.queryCancelled = false;

                this.InitViews();
                Tuple<SessionHistoryDocumentResponse, string> queryResponse = null;

                string eToken = await ToolAuthentication.GetDevTokenSilentlyAsync(this.tbScid.Text, this.tbSandbox.Text);

                this.lblExplanation.Text = "Downloading change history for the selected historical\nMPSD document...";
                if (this.listView1.SelectedIndices.Count == 1)
                {
                    var index = this.listView1.SelectedIndices[0];
                    queryResponse = await this.QueryForDocSessionHistoryAsync(eToken, this.listView1.Items[index].SubItems[0].Text, this.listView1.Items[index].SubItems[1].Text).ConfigureAwait(true);
                }

                this.downloadPanel.Hide();

                if (queryResponse.Item2 != null)
                {
                    MessageBox.Show(string.Format("{0}\n\nThe server is busy.\nPlease try again.", queryResponse.Item2), QueryFailure);
                    return;
                }

                if (queryResponse != null && queryResponse.Item1 != null && queryResponse.Item1.Results != null)
                {
                    queryResponse.Item1.Results.Sort((foo1, foo2) => foo1.ChangeNumber.CompareTo(foo2.ChangeNumber));

                    foreach (var item in queryResponse.Item1.Results)
                    {
                        string[] lv2arr = new string[DocumentMetadataColumnCount] 
                            { 
                                item.ChangeNumber == SessionHistory.MaxChangeValue ? "expired" : item.ChangeNumber.ToString(),
                                item.ChangedBy,
                                this.displayDateTimesAsUTCToolStripMenuItem.Checked ? item.Timestamp.ToString() : item.Timestamp.ToLocalTime().ToString(),
                                item.TitleId,
                                item.ServiceId,
                                item.CorrelationId,
                                item.Details
                            };

                        ListViewItem lvi2 = new ListViewItem(lv2arr);
                        this.listView2.Items.Add(lvi2);
                    }

                    this.DisplayChangesInfo();
                }
            }
        }

        private void DisplayChangesInfo()
        {
            string extraHelp = string.Empty;
            int numItems = this.listView2.Items.Count;

            if (numItems == 1)
            {
                extraHelp = "[Select the change below to see its full session snapshot.]";
            }
            else if (numItems > 1)
            {
                extraHelp = "[Ctrl+Select two changes below to see both snapshots side by side]";
            }

            this.lblChangeCount.Text = string.Format("{0} change{1}     {2}", numItems, numItems != 1 ? "s" : string.Empty, extraHelp);
        }

        private async Task<Tuple<SessionHistoryDocumentResponse, string>> QueryForDocSessionHistoryAsync(string eToken, string sessionName, string branch)
        {
            SessionHistoryDocumentResponse queryResponse = null;
            string errMsg = null;

            var response = await SessionHistory.GetSessionHistoryDocumentDataAsync(
                this.tbScid.Text,
                this.tbTemplateName.Text,
                sessionName, 
                branch,
                eToken);

            if (response.Item1 != System.Net.HttpStatusCode.OK)
            {
                errMsg = response.Item2;
            }
            else
            {
                queryResponse = new SessionHistoryDocumentResponse(response.Item2);
            }

            return new Tuple<SessionHistoryDocumentResponse, string>(queryResponse, errMsg);
        }

        private async void ListView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listView2.SelectedIndices.Count > 2)
            {
                return;
            }

            this.rtbSnapshotLeft.Text = string.Empty;
            this.rtbSnapshotRight.Text = string.Empty;
            this.lblChangeLeft.Text = string.Empty;
            this.lblChangeRight.Text = string.Empty;

            if (this.listView2.SelectedIndices.Count > 0)
            {
                int index = 0;
                if (this.listView1.SelectedItems.Count > 0)
                {
                    index = this.listView1.SelectedIndices[0];
                }
                
                var leftIndex = this.listView2.SelectedIndices[0];
                long changeLeft;

                string errMsg = null;

                string leftSnapshot = string.Empty;
                string rightSnapshot = string.Empty;

                if (long.TryParse(this.listView2.Items[leftIndex].SubItems[0].Text, out changeLeft))
                {
                    Tuple<string, string> getSnapshotResponse = await this.QueryForDocSessionHistoryChangeAsync(
                        this.listView1.Items[index].SubItems[0].Text,
                        this.listView1.Items[index].SubItems[1].Text,
                        changeLeft).ConfigureAwait(true);

                    if (getSnapshotResponse.Item1 != null)
                    {
                        this.lblChangeLeft.Text = string.Format("Change #{0}", changeLeft);
                        leftSnapshot = getSnapshotResponse.Item1;
                    }
                    else
                    {
                        errMsg = getSnapshotResponse.Item2;
                    }
                }

                if (errMsg == null && this.listView2.SelectedIndices.Count > 1)
                {
                    long changeRight;
                    var rightIndex = this.listView2.SelectedIndices[1];

                    if (long.TryParse(this.listView2.Items[rightIndex].SubItems[0].Text, out changeRight))
                    {
                        if (!this.showingOfflineSession)
                        {
                            this.lblExplanation.Text = string.Format("Downloading change #{0}", changeRight);
                        }

                        Tuple<string, string> getSnapshotResponse = await this.QueryForDocSessionHistoryChangeAsync(
                            this.listView1.Items[index].SubItems[0].Text,
                            this.listView1.Items[index].SubItems[1].Text,
                            changeRight).ConfigureAwait(true);

                        if (getSnapshotResponse.Item1 != null)
                        {
                            rightSnapshot = getSnapshotResponse.Item1;
                            this.lblChangeRight.Text = string.Format("Change #{0}", changeRight);
                        }
                        else
                        {
                            errMsg = getSnapshotResponse.Item2;
                        }
                    }
                }

                this.ShowDiffs(leftSnapshot, rightSnapshot);

                this.downloadPanel.Hide();

                if (errMsg != null)
                {
                    MessageBox.Show(string.Format("{0}\n\nThe server is busy.\nPlease try again.", errMsg), QueryFailure);
                }
            }
        }

        private void DisplayDiffPiece(RichTextBox box, IList<DiffPiece> diffPieces, bool monoChrome)
        {
            box.Clear();
            Color color = Color.White;

            foreach (DiffPiece diffPiece in diffPieces)
            {
                if (diffPiece.Text != null)
                {
                    if (diffPiece.Position.HasValue)
                    {
                        box.AppendText(string.Format("{0, 5} ", diffPiece.Position.Value.ToString()), Color.White);
                    }

                    if (!monoChrome)
                    {
                        switch (diffPiece.Type)
                        {
                            case ChangeType.Deleted:
                                color = Color.IndianRed;
                                break;

                            case ChangeType.Inserted:
                                color = Color.LightYellow;
                                break;

                            case ChangeType.Modified:
                                color = Color.LightCyan;
                                break;

                            case ChangeType.Imaginary:
                                color = Color.LightCoral;
                                break;

                            case ChangeType.Unchanged:
                                color = Color.White;
                                break;
                        }
                    }

                    box.AppendText(string.Format("{0}\n", diffPiece.Text.PadRight(500)), color);
                }
            }
        }

        private void ShowDiffs(string oldText, string newText)
        {
            IDiffer differ = new Differ();

            DiffResult diffResult = differ.CreateLineDiffs(oldText, newText, false);

            ISideBySideDiffBuilder diffBuilder = new SideBySideDiffBuilder(differ);

            SideBySideDiffModel model = diffBuilder.BuildDiffModel(oldText, newText);

            this.DisplayDiffPiece(this.rtbSnapshotLeft, model.OldText.Lines, string.IsNullOrEmpty(newText));
            this.DisplayDiffPiece(this.rtbSnapshotRight, model.NewText.Lines, false);
        }

        private async Task<Tuple<string, string>> QueryForDocSessionHistoryChangeAsync(string sessionName, string branch, long changeNumber)
        {
            string snapshot = null;
            string errorMsg = null;

            if (changeNumber == SessionHistory.MaxChangeValue)
            {
                return new Tuple<string, string>(null, null); // there is nothing to get, so don't bother trying
            }

            string hashKey = this.snapshotCache.GetHashString(
                sessionName,
                branch,
                changeNumber);

            if (!this.snapshotCache.TryGetSnapshot(hashKey, out snapshot))
            {
                string eToken = await ToolAuthentication.GetDevTokenSilentlyAsync(this.tbScid.Text, this.tbSandbox.Text);

                this.lblExplanation.Text = string.Format("Downloading session snapshot #{0}", changeNumber);

                var response = await SessionHistory.GetSessionHistoryDocumentChangeAsync(
                    this.tbScid.Text,
                    this.tbTemplateName.Text,
                    sessionName,
                    branch,
                    changeNumber,
                    eToken);

                if (response.Item1 == HttpStatusCode.OK)
                {
                    snapshot = response.Item2;
                    this.snapshotCache.AddSnapshotToCache(hashKey, response.Item2);
                }
                else if (response.Item1 != HttpStatusCode.NoContent)
                {
                    errorMsg = response.Item2;
                }
            }

            return new Tuple<string, string>(snapshot, errorMsg);
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string msgTitle = "MPSD Session History Viewer";
            string msgBody = "\u00a9 2015 Microsoft Corporation";

            MessageBox.Show(msgBody, msgTitle);
        }

        private async Task SearchForHistoricalDocumentsAsync(QueryBundle queryBundle, bool addToStack)
        {
            this.tbSandbox.Text = queryBundle.Sandbox;
            this.tbScid.Text = queryBundle.Scid;
            this.tbTemplateName.Text = queryBundle.TemplateName;
            this.tbQueryKey.Text = queryBundle.QueryKey;
            this.cmbQueryType.SelectedIndex = queryBundle.QueryKeyIndex;
            this.dateTimePicker1.Value = queryBundle.QueryFrom;
            this.dateTimePicker2.Value = queryBundle.QueryTo;

            string eToken = await ToolAuthentication.GetDevTokenSilentlyAsync(this.tbScid.Text, this.tbSandbox.Text);

            this.lblExplanation.Text = "Searching for MPSD historical documents...";

            Tuple<HttpStatusCode, string> response = null;
            string continuationToken = null;

            do
            {
                response = await this.QueryForHistoricalSessionDocuments(eToken, queryBundle, continuationToken);

                if (response.Item1 == HttpStatusCode.OK)
                {
                    continuationToken = response.Item2;
                }
                else
                {
                    continuationToken = null;
                }
            }
            while (continuationToken != null);

            this.downloadPanel.Hide();

            if (response.Item1 != HttpStatusCode.OK)
            {
                if (response.Item1 == HttpStatusCode.Unauthorized)
                {
                    MessageBox.Show("Your auth token has expired. Re-try the query to get a new token", "Expired Token");
                }
                else if (response.Item1 == HttpStatusCode.InternalServerError)
                {
                    MessageBox.Show("The server is busy.  Please try again", QueryFailure);
                }
                else
                {
                    MessageBox.Show(response.Item2, QueryFailure);
                }
            }
            else
            {
                if (this.listView1.Items.Count == 0)
                {
                    this.btnPriorQuery.Visible = this.queryStack.Count > 0; // show the back button following un unsuccesful query (if there was a prior successul one)
                    MessageBox.Show("No results found.  Try expanding the query time window (if possible)\nor use different search criteria.", "Query Results");
                }
                else
                {
                    if (addToStack)
                    {
                        this.queryStack.Push(queryBundle); // succesful query, so remember it
                        this.btnPriorQuery.Visible = this.queryStack.Count > 1;
                    }
                    else
                    {
                        this.btnPriorQuery.Visible = this.queryStack.Count > 0;
                    }
                }
            }
        }

        private async void BtnPriorQuery_Click(object sender, EventArgs e)
        {
            QueryBundle priorQuery = this.queryStack.Pop();

            await this.SearchForHistoricalDocumentsAsync(priorQuery, false);
        }

        private async void BtnQuery_Click(object sender, EventArgs e)
        {
            this.queryCancelled = false;

            if (!this.InputsAreValid())
            {
                return;
            }

            this.saveSessionHistoryToolStripMenuItem.Visible = true;
            this.showingOfflineSession = false;

            QueryBundle bundle = new QueryBundle()
            {
                Sandbox = this.tbSandbox.Text.Trim(),
                Scid = this.tbScid.Text.Trim(),
                TemplateName = this.tbTemplateName.Text.Trim(),
                QueryKey = this.tbQueryKey.Text.Trim(),
                QueryKeyIndex = this.cmbQueryType.SelectedIndex,
                QueryFrom = this.dateTimePicker1.Value,
                QueryTo = this.dateTimePicker2.Value,
            };

            await this.SearchForHistoricalDocumentsAsync(bundle, true);
        }

        private void SendFeedbackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(string.Format("This tool will improve with your detailed feedback.\n\nPlease send comments, feature requests, and bug reports\nto {0}.", FeedbackEmailAddress), "Feedback Request");
        }

        private void TbSandbox_Validated(object sender, EventArgs e)
        {
            this.userSettings.Sandbox = this.tbSandbox.Text;
        }

        private void TbScid_Validated(object sender, EventArgs e)
        {
            this.userSettings.Scid = this.tbScid.Text;
        }

        private void TbTemplateName_Validated(object sender, EventArgs e)
        {
            this.userSettings.TemplateName = this.tbTemplateName.Text;
        }

        private void TbQueryKey_Validated(object sender, EventArgs e)
        {
            this.userSettings.QueryKey = this.tbQueryKey.Text;
        }

        private void CmbQueryType_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.userSettings.QueryType = this.cmbQueryType.SelectedIndex;
        }

        private void AdjustControls()
        {
            const int NumSnapshotViews = 2;

            if (ActiveForm != null)
            {
                int activeFormHeight = ActiveForm.Height;
                int activeFormWidth = ActiveForm.Width;

                // width calculations
                this.listView1.Width = activeFormWidth - (SideMargin * 3);
                this.splitContainer1.Width = this.listView1.Width;

                this.listView2.Width = this.splitContainer1.Width;

                int snapshotWidth = (this.splitContainer1.Panel2.Width - SideMargin ) / NumSnapshotViews;
                this.rtbSnapshotLeft.Width = snapshotWidth;
                this.rtbSnapshotRight.Width = snapshotWidth;

                // control height calcalations
                this.splitContainer1.Height = activeFormHeight - this.listView1.Bottom - (VertMargin * 2);
                this.AdjustPanel2Controls();

                // vertical position adjustment
                this.splitContainer1.Top = this.listView1.Bottom + VertMargin;

                // horizontal position adjustments
                this.rtbSnapshotRight.Left = this.rtbSnapshotLeft.Right + SideMargin;
                this.lblChangeRight.Left = this.rtbSnapshotRight.Left;
                this.checkboxLockVScroll.Left = (activeFormWidth / 2) - (this.checkboxLockVScroll.Width / 2);
            }
        }

        private void AdjustPanel2Controls()
        {
            int snapshotHeight = this.splitContainer1.Panel2.Height - this.lblChangeLeft.Height - this.checkboxLockVScroll.Height - (VertMargin * 2);

            this.rtbSnapshotLeft.Height = snapshotHeight - 50;
            this.rtbSnapshotRight.Height = snapshotHeight - 50;

            this.checkboxLockVScroll.Top = this.rtbSnapshotLeft.Bottom + VertMargin;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.AdjustControls();
        }

        private void Form1_ClientSizeChanged(object sender, EventArgs e)
        {
            this.AdjustControls();
        }

        private void RtbSnapshotLeft_VScroll(object sender, EventArgs e)
        {
            if (this.checkboxLockVScroll.Checked && !vRightScrollInProgress)
            {
                vLeftScrollInProgress = true;

                int charPos = this.rtbSnapshotLeft.GetCharIndexFromPosition(new Point(0, 0));
                this.rtbSnapshotRight.Select(charPos, 1);
                this.rtbSnapshotRight.ScrollToCaret();

                vLeftScrollInProgress = false;
            }
        }

        private void RtbSnapshotRight_VScroll(object sender, EventArgs e)
        {
            var offset = this.rtbSnapshotRight.AutoScrollOffset;

            if (this.checkboxLockVScroll.Checked && !vLeftScrollInProgress)
            {
                vRightScrollInProgress = true;

                int charPos = this.rtbSnapshotRight.GetCharIndexFromPosition(new Point(0, 0));
                this.rtbSnapshotLeft.Select(charPos, 1);
                this.rtbSnapshotLeft.ScrollToCaret();

                vRightScrollInProgress = false;
            }
        }

        private void RtbSnapshotRight_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.checkboxLockVScroll.Checked = !this.checkboxLockVScroll.Checked;
            }
        }

        private void RtbSnapshotLeft_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.checkboxLockVScroll.Checked = !this.checkboxLockVScroll.Checked;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // save any UI customizations the user made
            string lv1ColumnWidths = string.Format(
                "{0},{1},{2},{3},{4},{5}", 
                this.listView1.Columns[0].Width,
                this.listView1.Columns[1].Width,
                this.listView1.Columns[2].Width,
                this.listView1.Columns[3].Width,
                this.listView1.Columns[4].Width,
                this.listView1.Columns[5].Width);

            string lv2ColumnWidths = string.Format(
                "{0},{1},{2},{3},{4},{5},{6}", 
                this.listView2.Columns[0].Width,
                this.listView2.Columns[1].Width,
                this.listView2.Columns[2].Width,
                this.listView2.Columns[3].Width,
                this.listView2.Columns[4].Width,
                this.listView2.Columns[5].Width,
                this.listView2.Columns[6].Width);

            this.userSettings.ListView1ColumnWidths = lv1ColumnWidths;
            this.userSettings.ListView2ColumnWidths = lv2ColumnWidths;
        }

        private void SplitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            this.listView2.Height = this.splitContainer1.Panel1.Height - this.lblChangeCount.Height - VertMargin;

            this.AdjustPanel2Controls();
        }

        private void DisplayDateTimesAsUTCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.userSettings.ShowLocalTime = !this.displayDateTimesAsUTCToolStripMenuItem.Checked;

            this.listView1.Columns[3].Text = this.displayDateTimesAsUTCToolStripMenuItem.Checked ? UtcTimeColumnHeader : LocalTimeColumnHeader;
            foreach (ListViewItem lvi1 in this.listView1.Items)
            {
                // column3 is the timestamp
                DateTime dtToModify = DateTime.Parse(lvi1.SubItems[3].Text);
                lvi1.SubItems[3].Text = this.displayDateTimesAsUTCToolStripMenuItem.Checked ? dtToModify.ToUniversalTime().ToString() : dtToModify.ToLocalTime().ToString();
            }

            this.listView2.Columns[2].Text = this.displayDateTimesAsUTCToolStripMenuItem.Checked ? UtcTimeColumnHeader : LocalTimeColumnHeader;
            foreach (ListViewItem lvi2 in this.listView2.Items)
            {
                // column 2 is the timestamp
                DateTime dtToModify = DateTime.Parse(lvi2.SubItems[2].Text);
                lvi2.SubItems[2].Text = this.displayDateTimesAsUTCToolStripMenuItem.Checked ? dtToModify.ToUniversalTime().ToString() : dtToModify.ToLocalTime().ToString();
            }
        }

        private void LoadSessionHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HistoricalDocument document = null;

            var openFileDialog = new OpenFileDialog()
            {
                Filter = SessionHistoryFileFilter
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (StreamReader sr = new StreamReader(openFileDialog.FileName))
                {
                    string fileContents = sr.ReadToEnd();
                    try
                    {
                        document = JsonConvert.DeserializeObject<HistoricalDocument>(fileContents);
                    }
                    catch (JsonSerializationException)
                    {
                        MessageBox.Show("Could not deserialize the file to a\nHistorical Document.", "Load Document Error");
                    }
                }
            }

            if (document != null)
            {
                this.listView1.Items.Clear();
                this.InitViews();

                string[] arr = new string[QueryResultColumnCount] 
                { 
                    document.SessionName,
                    document.Branch,
                    document.NumSnapshots.ToString(),
                    document.LastModified,
                    document.IsExpired.ToString(),
                    document.ActivityId
                };

                ListViewItem lvi = new ListViewItem(arr);
                this.listView1.Items.Add(lvi);

                foreach (DocumentStateSnapshot snapshot in document.DocumentSnapshots)
                {
                    string hashKey = this.snapshotCache.GetHashString(
                        document.SessionName,
                        document.Branch,
                        snapshot.Change);

                    string snapshotBody;

                    if (snapshot.Change != SessionHistory.MaxChangeValue)
                    {
                        if (!this.snapshotCache.TryGetSnapshot(hashKey, out snapshotBody))
                        {
                            snapshotBody = snapshot.Body;
                            this.snapshotCache.AddSnapshotToCache(hashKey, snapshot.Body);
                        }
                    }

                    string[] lv2arr = new string[DocumentMetadataColumnCount] 
                        { 
                            snapshot.Change == SessionHistory.MaxChangeValue ? "expired" : snapshot.Change.ToString(),
                            snapshot.ModifiedByXuids,
                            snapshot.Timestamp.ToString(),
                            snapshot.TitleId,
                            snapshot.ServiceId,
                            snapshot.CorrelationId,
                            snapshot.ChangeDetails
                        };

                    ListViewItem lvi2 = new ListViewItem(lv2arr);
                    this.listView2.Items.Add(lvi2);
                }

                this.lblDocCount.Text = "1 document [offline]";
                this.DisplayChangesInfo();

                this.showingOfflineSession = true;
                this.saveSessionHistoryToolStripMenuItem.Visible = false;
            }
        }

        private void ListView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.listView1.SelectedItems.Count > 0)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    this.SessionDocumentContextMenuStrip.Show(Cursor.Position);
                }
            }
        }

        // ColumnClick event handler.
        internal void ListView1_ColumnClick(object o, ColumnClickEventArgs e)
        {
            // Set the ListViewItemSorter property to a new ListViewItemComparer 
            // object. Setting this property immediately sorts the 
            // ListView using the ListViewItemComparer object.            
            this.listView1.ListViewItemSorter = new ListViewItemComparer(e.Column, this.isLV1SortOrderDescending);
            this.isLV1SortOrderDescending = !this.isLV1SortOrderDescending;
        }

        // ColumnClick event handler.
        internal void ListView2_ColumnClick(object o, ColumnClickEventArgs e)
        {
            // Set the ListViewItemSorter property to a new ListViewItemComparer 
            // object. Setting this property immediately sorts the 
            // ListView using the ListViewItemComparer object.            
            this.listView2.ListViewItemSorter = new ListViewItemComparer(e.Column, this.isLV2SortOrderDescending);
            this.isLV2SortOrderDescending = !this.isLV2SortOrderDescending;
        }

        private async void SaveSessionHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count != 1)
            {
                return;
            }
            
            int selectedIndex = this.listView1.SelectedIndices[0];

            HistoricalDocument document = new HistoricalDocument()
            {
                SessionName = this.listView1.Items[selectedIndex].SubItems[0].Text,
                Branch = this.listView1.Items[selectedIndex].SubItems[1].Text,
                NumSnapshots = int.Parse(this.listView1.Items[selectedIndex].SubItems[2].Text),
                LastModified = this.listView1.Items[selectedIndex].SubItems[3].Text,
                IsExpired = bool.Parse(this.listView1.Items[selectedIndex].SubItems[4].Text),
                ActivityId = this.listView1.Items[selectedIndex].SubItems[5].Text,
            };

            string eToken = await ToolAuthentication.GetDevTokenSilentlyAsync(this.tbScid.Text, this.tbSandbox.Text);

            this.lblExplanation.Text = "Downloading change history for the selected session...";

            Tuple<SessionHistoryDocumentResponse, string> queryResponse = await this.QueryForDocSessionHistoryAsync(eToken, document.SessionName, document.Branch);

            if (queryResponse.Item2 != null)
            {
                this.downloadPanel.Hide();

                MessageBox.Show(string.Format("{0}\n\nThe server may have been busy.\nPlease try again.", queryResponse.Item2), "Error!");
                return;
            }

            string errMsg = null;

            if (queryResponse.Item1 != null)
            {
                foreach (var item in queryResponse.Item1.Results)
                {
                    Tuple<string, string> getSnapshotResponse = await this.QueryForDocSessionHistoryChangeAsync(document.SessionName, document.Branch, item.ChangeNumber);
                    string snapshotBody = getSnapshotResponse.Item1;
                    errMsg = getSnapshotResponse.Item2;

                    if (errMsg != null)
                    {
                        break;
                    }

                    DocumentStateSnapshot snapshot = new DocumentStateSnapshot()
                    {
                        Change = item.ChangeNumber,
                        ModifiedByXuids = item.ChangedBy,
                        Timestamp = item.Timestamp,
                        TitleId = item.TitleId,
                        ServiceId = item.ServiceId,
                        CorrelationId = item.CorrelationId,
                        ChangeDetails = item.Details,
                        Body = snapshotBody != null ? snapshotBody : string.Empty
                    };

                    document.DocumentSnapshots.Add(snapshot);
                }
            }

            this.downloadPanel.Hide();

            if (errMsg != null)
            {
                MessageBox.Show(string.Format("{0}\n\nPlease try again.", errMsg), "Error");
            }
            else
            {
                // serialize the HistoricalDocument to json
                string testString = JsonConvert.SerializeObject(document);

                var saveDialog = new SaveFileDialog()
                {
                    Filter = SessionHistoryFileFilter,
                    FileName = string.Format("{0}~{1}", document.SessionName, document.Branch)
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    using (StreamWriter sw = new StreamWriter(saveDialog.FileName))
                    {
                        sw.Write(testString);
                    }
                }
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.queryCancelled = true;
            this.downloadPanel.Hide();
        }

        private void DownloadPanel_VisibleChanged(object sender, EventArgs e)
        {
            if (this.downloadPanel.Visible == true)
            {
                // center the panel to the application
                this.downloadPanel.Left = (this.downloadPanel.Parent.Width / 2) - (this.downloadPanel.Width / 2);
                this.downloadPanel.Top = (this.downloadPanel.Parent.Height / 2) - (this.downloadPanel.Height / 2);
            }
        }

        private void LblExplanation_TextChanged(object sender, EventArgs e)
        {
            this.downloadPanel.Show();
        }

        private async void SingInout_Click(object sender, EventArgs e)
        {
            try
            {
                this.btnSingInout.Enabled = false;
                if (this.signedInuser != null)
                {
                    ToolAuthentication.SignOut();
                    this.signedInuser = null;
                }
                else
                {
                    DevAccountSource accountSource = DevAccountSource.WindowsDevCenter;
                    if (this.cmbAccountSource.SelectedIndex == 1)
                    {
                        accountSource = DevAccountSource.XboxDeveloperPortal;
                    }

                    this.signedInuser = await ToolAuthentication.SignInAsync(accountSource, null);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error");
            }
            finally
            {
                this.UpdateAccountPanel();
                this.btnSingInout.Enabled = true;
            }
        }

        private void UpdateAccountPanel()
        {
            this.signedInuser = ToolAuthentication.LoadLastSignedInUser();
            if (this.signedInuser != null)
            {
                this.btnSingInout.Text = "Sign Out";
                this.cmbAccountSource.Enabled = false;
                this.labelUserName.Text = this.signedInuser.Name;
                this.btnQuery.Enabled = true;
                this.cmbAccountSource.SelectedIndex =
                    this.signedInuser.AccountSource == DevAccountSource.WindowsDevCenter ? 0 : 1;
            }
            else
            {
                this.btnSingInout.Text = "Sign In";
                this.cmbAccountSource.Enabled = true;
                this.labelUserName.Text = string.Empty;
                this.btnQuery.Enabled = false;
                this.cmbAccountSource.SelectedIndex = -1;
            }
        }

        // Implements the manual sorting of items by columns.
        private class ListViewItemComparer : IComparer
        {
            private int col;
            private bool isDescending;

            public ListViewItemComparer()
            {
                this.col = 0;
            }

            public ListViewItemComparer(int column, bool isDescending)
            {
                this.col = column;
                this.isDescending = isDescending;
            }

            public int Compare(object x, object y)
            {
                long value1;
                bool isNumber = long.TryParse(((ListViewItem)x).SubItems[this.col].Text, out value1);

                if (!isNumber)
                {
                    if (this.isDescending)
                    {
                        return string.Compare(((ListViewItem)x).SubItems[this.col].Text, ((ListViewItem)y).SubItems[this.col].Text);
                    }
                    else
                    {
                        return string.Compare(((ListViewItem)y).SubItems[this.col].Text, ((ListViewItem)x).SubItems[this.col].Text);
                    }
                }
                else
                {
                    long value2;

                    if (string.IsNullOrWhiteSpace(((ListViewItem)y).SubItems[this.col].Text))
                    {
                        value2 = 0;
                    }
                    else
                    {
                        if (!long.TryParse(((ListViewItem)y).SubItems[this.col].Text, out value2))
                        {
                            value2 = SessionHistory.MaxChangeValue;
                        }
                    }

                    if (value1 == value2)
                    {
                        return 0;
                    }

                    if (this.isDescending)
                    {
                        if (value1 < value2)
                            return -1;
                        else
                            return 1;
                    }
                    else
                    {
                        if (value1 < value2)
                            return 1;
                        else
                            return -1;
                    }
                }
            }
        }
    }
}
