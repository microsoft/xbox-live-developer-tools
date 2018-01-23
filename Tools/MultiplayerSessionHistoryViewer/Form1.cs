//-----------------------------------------------------------------------
// <copyright file="Form1.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
//     Internal use only.
// </copyright>
//-----------------------------------------------------------------------

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

namespace SessionHistoryViewer
{
    public partial class Form1 : Form
    {
        private SessionHistorySnapshotCache SnapshotCache = new SessionHistorySnapshotCache();
        private static bool VLeftScrollInProgress;
        private static bool VRightScrollInProgress;
        private const string LocalTimeColumnHeader = "Timestamp (Local)";
        private const string UtcTimeColumnHeader = "Timestamp (UTC)";
        private const string feedbackEmailAddress = "multiplayer@microsoft.com";
        private const string sessionHistoryFileFilter = "Session History files (*.hist)|*.hist";
        private const string QueryFailure = "Query Failure";
        private const string SaveInstructions = "To save a session history offline, right click on an item below.";

        const int NumLookbackDays = 7;
        const int QueryResultColumnCount = 6;
        const int DocumentMetadataColumnCount = 7;

        const int sideMargin = 18;
        const int vertMargin = 4;

        UserSettings userSettings = new UserSettings();

        Stack<QueryBundle> queryStack = new Stack<QueryBundle>();
        bool ShowingOfflineSession;
        bool QueryCancelled;
        bool isLV1SortOrderDescending = true;
        bool isLV2SortOrderDescending = true;

        private DevAccount signedInuser = null;

        public Form1()
        {
            InitializeComponent();
            
            listView1.View = View.Details;
            listView1.GridLines = true;
            listView1.FullRowSelect = true;

            string lv1ColumnWidths = userSettings.ListView1ColumnWidths;
            string[] c1Widths = lv1ColumnWidths.Split(new char[] { ',' });

            displayDateTimesAsUTCToolStripMenuItem.Checked = userSettings.ShowLocalTime;

            listView1.AddColumn("SessionName", c1Widths, 0, 231);
            listView1.AddColumn("Branch", c1Widths, 1, 231);
            listView1.AddColumn("Changes", c1Widths, 2, 55);            
            listView1.AddColumn(userSettings.ShowLocalTime ? LocalTimeColumnHeader : UtcTimeColumnHeader, c1Widths, 3, 140);
            listView1.AddColumn("Expired?", c1Widths, 4, 65);
            listView1.AddColumn("ActivityId", c1Widths, 5, 250);
            
            listView2.View = View.Details;
            listView2.GridLines = true;
            listView2.FullRowSelect = true;

            string lv2ColumnWidths = userSettings.ListView2ColumnWidths;
            string[] c2Widths = lv2ColumnWidths.Split(new char[] { ',' });

            listView2.AddColumn("Change", c2Widths, 0, 50);
            listView2.AddColumn("Modified By", c2Widths, 1, 120);
            listView2.AddColumn(userSettings.ShowLocalTime ? LocalTimeColumnHeader : UtcTimeColumnHeader, c2Widths, 2, 120);
            listView2.AddColumn("TitleId", c2Widths, 3, 80);
            listView2.AddColumn("ServiceId", c2Widths, 4, 100);
            listView2.AddColumn("CorrelationId", c2Widths, 5, 250);
            listView2.AddColumn("Changes", c2Widths, 6, 450);
            
            dateTimePicker1.MinDate = DateTime.Today.AddDays(-NumLookbackDays);
            dateTimePicker1.MaxDate = DateTime.Today;

            dateTimePicker2.MinDate = DateTime.Today.AddDays(-NumLookbackDays);
            dateTimePicker2.MaxDate = DateTime.Today.AddDays(1);

            dateTimePicker1.Value = DateTime.Today.AddDays(-NumLookbackDays);
            dateTimePicker2.Value = DateTime.Today;

            tbSandbox.Text = userSettings.Sandbox;
            tbScid.Text = userSettings.Scid;
            tbTemplateName.Text = userSettings.TemplateName;
            tbQueryKey.Text = userSettings.QueryKey;
            cmbQueryType.SelectedIndex = userSettings.QueryType;
            cmbAccountSource.SelectedIndex = userSettings.AccountSource;

            UpdateAccountPanel();
        }

        private async Task<Tuple<HttpStatusCode, string>> QueryForHistoricalSessionDocuments(string eToken, QueryBundle queryBundle, string continuationToken)
        {
            if (continuationToken == null)
            {
                InitViews();
                listView1.Items.Clear();
            }

            Tuple<System.Net.HttpStatusCode, string> response = null;

            switch (cmbQueryType.SelectedIndex)
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
                            tbQueryKey.Text,
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
                    item.sessionName,
                    item.branch,
                    item.changes.ToString(),
                    displayDateTimesAsUTCToolStripMenuItem.Checked ? item.lastModified.ToString() : item.lastModified.ToLocalTime().ToString(),
                    item.isExpired.ToString(),
                    item.activityId
                };

                ListViewItem lvi = new ListViewItem(arr);
                listView1.Items.Add(lvi);
            }

            lblDocCount.Text = string.Format("{0} session{1}", listView1.Items.Count, (listView1.Items.Count != 1) ? "s" : "");
            lblDocCount.Text += "      [" + SaveInstructions + "]";

            if (QueryCancelled)
            {
                return new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, null); // ignore continuation token if user cancelled the query
            }
            else
            {
                return new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, queryResponse.continuationToken);
            }
        }

        private void InitViews()
        {
            listView2.Items.Clear();
            rtbSnapshotLeft.Text = string.Empty;
            rtbSnapshotRight.Text = string.Empty;
            lblChangeLeft.Text = string.Empty;
            lblChangeRight.Text = string.Empty;
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
            if (!ValidateControl(tbSandbox)) return false;

            Guid guid;
            if (!Guid.TryParseExact(tbScid.Text, "D", out guid))
            {
                Console.Beep();
                tbScid.BackColor = Color.FromKnownColor(KnownColor.Aquamarine);
                tbScid.Focus();
                return false;
            }
            tbScid.BackColor = Color.FromKnownColor(KnownColor.Window);

            if (!ValidateControl(tbTemplateName)) return false;
            if (!ValidateControl(tbQueryKey)) return false;

            if (cmbQueryType.SelectedIndex == (int)QueryBy.GamerXuid)
            {
                long xuid;
                if (!long.TryParse(tbQueryKey.Text, out xuid))
                {
                    Console.Beep();
                    tbQueryKey.BackColor = Color.FromKnownColor(KnownColor.Aquamarine);
                    tbQueryKey.Focus();
                    return false;
                }
            }

            return true;
        }

        private async void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                return;
            }

            if (!ShowingOfflineSession)
            {
                QueryCancelled = false;

                InitViews();
                Tuple<SessionHistoryDocumentResponse, string> queryResponse = null;

                string eToken = await Authentication.GetDevTokenSlientlyAsync(tbScid.Text, tbSandbox.Text);

                lblExplanation.Text = "Downloading change history for the selected historical\nMPSD document...";
                if (listView1.SelectedIndices.Count == 1)
                {
                    var index = listView1.SelectedIndices[0];
                    queryResponse = await QueryForDocSessionHistoryAsync(eToken, listView1.Items[index].SubItems[0].Text, listView1.Items[index].SubItems[1].Text).ConfigureAwait(true);
                }

                downloadPanel.Hide();

                if (queryResponse.Item2 != null)
                {
                    MessageBox.Show(string.Format("{0}\n\nThe server is busy.\nPlease try again.", queryResponse.Item2), QueryFailure);
                    return;
                }

                if (queryResponse != null && queryResponse.Item1 != null && queryResponse.Item1.Results != null)
                {
                    queryResponse.Item1.Results.Sort((foo1, foo2) => foo1.changeNumber.CompareTo(foo2.changeNumber));

                    foreach (var item in queryResponse.Item1.Results)
                    {
                        string[] lv2arr = new string[DocumentMetadataColumnCount] 
                            { 
                                item.changeNumber == SessionHistory.MaxChangeValue ? "expired" : item.changeNumber.ToString(),
                                item.changedBy,
                                displayDateTimesAsUTCToolStripMenuItem.Checked ? item.timestamp.ToString() : item.timestamp.ToLocalTime().ToString(),
                                item.titleId,
                                item.serviceId,
                                item.correlationId,
                                item.details
                            };

                        ListViewItem lvi2 = new ListViewItem(lv2arr);
                        listView2.Items.Add(lvi2);
                    }

                    DisplayChangesInfo();
                }
            }
        }

        private void DisplayChangesInfo()
        {
            string extraHelp = "";
            int numItems = listView2.Items.Count;

            if (numItems == 1)
            {
                extraHelp = "[Select the change below to see its full session snapshot.]";
            }
            else if (numItems > 1)
            {
                extraHelp = "[Ctrl+Select two changes below to see both snapshots side by side]";
            }

            lblChangeCount.Text = string.Format("{0} change{1}     {2}", numItems, numItems != 1 ? "s" : "", extraHelp);
        }

        private async Task<Tuple<SessionHistoryDocumentResponse, string>> QueryForDocSessionHistoryAsync(string eToken, string sessionName, string branch)
        {
            SessionHistoryDocumentResponse queryResponse = null;
            string errMsg = null;

            var response = await SessionHistory.GetSessionHistoryDocumentDataAsync(
                tbScid.Text,
                tbTemplateName.Text,
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

        private async void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView2.SelectedIndices.Count > 2)
            {
                return;
            }

            rtbSnapshotLeft.Text = string.Empty;
            rtbSnapshotRight.Text = string.Empty;
            lblChangeLeft.Text = string.Empty;
            lblChangeRight.Text = string.Empty;

            if (listView2.SelectedIndices.Count > 0)
            {
                int index = 0;
                if (listView1.SelectedItems.Count > 0)
                {
                    index = listView1.SelectedIndices[0];
                }
                
                var leftIndex = listView2.SelectedIndices[0];
                long changeLeft;

                string errMsg = null;

                string leftSnapshot = string.Empty;
                string rightSnapshot = string.Empty;

                if (long.TryParse(listView2.Items[leftIndex].SubItems[0].Text, out changeLeft))
                {
                    Tuple<string, string> getSnapshotResponse = await QueryForDocSessionHistoryChangeAsync(
                        listView1.Items[index].SubItems[0].Text, 
                        listView1.Items[index].SubItems[1].Text,
                        changeLeft).ConfigureAwait(true);

                    if (getSnapshotResponse.Item1 != null)
                    {
                        lblChangeLeft.Text = string.Format("Change #{0}", changeLeft);
                        leftSnapshot = getSnapshotResponse.Item1;
                    }
                    else
                    {
                        errMsg = getSnapshotResponse.Item2;
                    }
                }

                if (errMsg == null && listView2.SelectedIndices.Count > 1)
                {
                    long changeRight;
                    var rightIndex = listView2.SelectedIndices[1];

                    if (long.TryParse(listView2.Items[rightIndex].SubItems[0].Text, out changeRight))
                    {
                        if (!ShowingOfflineSession)
                        {
                            lblExplanation.Text = string.Format("Downloading change #{0}", changeRight);
                        }

                        Tuple<string, string> getSnapshotResponse = await QueryForDocSessionHistoryChangeAsync(
                            listView1.Items[index].SubItems[0].Text, 
                            listView1.Items[index].SubItems[1].Text,
                            changeRight).ConfigureAwait(true);

                        if (getSnapshotResponse.Item1 != null)
                        {
                            rightSnapshot = getSnapshotResponse.Item1;                            
                            lblChangeRight.Text = string.Format("Change #{0}", changeRight);
                        }
                        else
                        {
                            errMsg = getSnapshotResponse.Item2;
                        }
                    }
                }

                ShowDiffs(leftSnapshot, rightSnapshot);

                downloadPanel.Hide();

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

            DisplayDiffPiece(rtbSnapshotLeft, model.OldText.Lines, string.IsNullOrEmpty(newText));
            DisplayDiffPiece(rtbSnapshotRight, model.NewText.Lines, false);
        }

        private async Task<Tuple<string, string>> QueryForDocSessionHistoryChangeAsync(string sessionName, string branch, long changeNumber)
        {
            string snapshot = null;
            string errorMsg = null;

            if (changeNumber == SessionHistory.MaxChangeValue)
            {
                return new Tuple<string, string>(null, null); // there is nothing to get, so don't bother trying
            }

            string hashKey = SnapshotCache.GetHashString(
                sessionName,
                branch,
                changeNumber);

            if (!SnapshotCache.TryGetSnapshot(hashKey, out snapshot))
            {

                string eToken = await Authentication.GetDevTokenSlientlyAsync(tbScid.Text, tbSandbox.Text);

                lblExplanation.Text = string.Format("Downloading session snapshot #{0}", changeNumber);

                var response = await SessionHistory.GetSessionHistoryDocumentChangeAsync(
                    tbScid.Text,
                    tbTemplateName.Text,
                    sessionName,
                    branch,
                    changeNumber,
                    eToken);

                if (response.Item1 == HttpStatusCode.OK)
                {
                    snapshot = response.Item2;
                    SnapshotCache.AddSnapshotToCache(hashKey, response.Item2);
                }
                else if (response.Item1 != HttpStatusCode.NoContent)
                {
                    errorMsg = response.Item2;
                }
            }

            return new Tuple<string,string>(snapshot, errorMsg);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string msgTitle = "MPSD Session History Viewer";
            string msgBody = "\u00a9 2015 Microsoft Corporation";

            MessageBox.Show(msgBody, msgTitle);
        }

        private async Task SearchForHistoricalDocumentsAsync(QueryBundle queryBundle, bool addToStack)
        {
            tbSandbox.Text = queryBundle.Sandbox;
            tbScid.Text = queryBundle.Scid;
            tbTemplateName.Text = queryBundle.TemplateName;
            tbQueryKey.Text = queryBundle.QueryKey;
            cmbQueryType.SelectedIndex = queryBundle.QueryKeyIndex;
            dateTimePicker1.Value = queryBundle.QueryFrom;
            dateTimePicker2.Value = queryBundle.QueryTo;

            string eToken = await Authentication.GetDevTokenSlientlyAsync(tbScid.Text, tbSandbox.Text);

            lblExplanation.Text = "Searching for MPSD historical documents...";

            Tuple<HttpStatusCode, string> response = null;
            string continuationToken = null;

            do
            {
                response = await QueryForHistoricalSessionDocuments(eToken, queryBundle, continuationToken);

                if (response.Item1 == HttpStatusCode.OK)
                {
                    continuationToken = response.Item2;
                }
                else
                {
                    continuationToken = null;
                }
            } while (continuationToken != null);

            downloadPanel.Hide();

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
                if (listView1.Items.Count == 0)
                {
                    btnPriorQuery.Visible = (queryStack.Count > 0); // show the back button following un unsuccesful query (if there was a prior successul one)
                    MessageBox.Show("No results found.  Try expanding the query time window (if possible)\nor use different search criteria.", "Query Results");
                }
                else
                {
                    if (addToStack)
                    {
                        queryStack.Push(queryBundle); // succesful query, so remember it
                        btnPriorQuery.Visible = (queryStack.Count > 1);
                    }
                    else
                    {
                        btnPriorQuery.Visible = (queryStack.Count > 0);
                    }
                }
            }
        }

        private async void btnPriorQuery_Click(object sender, EventArgs e)
        {
            QueryBundle priorQuery = queryStack.Pop();

            await SearchForHistoricalDocumentsAsync(priorQuery, false);
        }

        private async void btnQuery_Click(object sender, EventArgs e)
        {
            QueryCancelled = false;

            if (!InputsAreValid())
            {
                return;
            }

            saveSessionHistoryToolStripMenuItem.Visible = true;
            ShowingOfflineSession = false;

            QueryBundle bundle = new QueryBundle()
            {
                Sandbox = tbSandbox.Text.Trim(),
                Scid = tbScid.Text.Trim(),
                TemplateName = tbTemplateName.Text.Trim(),
                QueryKey = tbQueryKey.Text.Trim(),
                QueryKeyIndex = cmbQueryType.SelectedIndex,
                QueryFrom = dateTimePicker1.Value,
                QueryTo = dateTimePicker2.Value,
            };

            await SearchForHistoricalDocumentsAsync(bundle, true);
        }

        private void sendFeedbackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(string.Format("This tool will improve with your detailed feedback.\n\nPlease send comments, feature requests, and bug reports\nto {0}.", feedbackEmailAddress), "Feedback Request");
        }

        private void tbSandbox_Validated(object sender, EventArgs e)
        {
            userSettings.Sandbox = tbSandbox.Text;
        }

        private void tbScid_Validated(object sender, EventArgs e)
        {
            userSettings.Scid = tbScid.Text;
        }

        private void tbTemplateName_Validated(object sender, EventArgs e)
        {
            userSettings.TemplateName = tbTemplateName.Text;
        }

        private void tbQueryKey_Validated(object sender, EventArgs e)
        {
            userSettings.QueryKey = tbQueryKey.Text;
        }

        private void cmbQueryType_SelectedIndexChanged(object sender, EventArgs e)
        {
            userSettings.QueryType = cmbQueryType.SelectedIndex;
        }

        private void AdjustControls()
        {
            const int NumSnapshotViews = 2;

            if (ActiveForm != null)
            {
                int activeFormHeight = ActiveForm.Height;
                int activeFormWidth = ActiveForm.Width;

                // width calculations
                listView1.Width = (activeFormWidth - (sideMargin * 3));
                splitContainer1.Width = listView1.Width;

                listView2.Width = splitContainer1.Width;

                int snapshotWidth = (splitContainer1.Panel2.Width - sideMargin ) / NumSnapshotViews;
                rtbSnapshotLeft.Width = snapshotWidth;
                rtbSnapshotRight.Width = snapshotWidth;

                // control height calcalations
                splitContainer1.Height = activeFormHeight - listView1.Bottom - (vertMargin * 2);
                AdjustPanel2Controls();

                // vertical position adjustment
                splitContainer1.Top = listView1.Bottom + vertMargin;

                // horizontal position adjustments
                rtbSnapshotRight.Left = rtbSnapshotLeft.Right + sideMargin;
                lblChangeRight.Left = rtbSnapshotRight.Left;
                checkboxLockVScroll.Left = activeFormWidth / 2 - checkboxLockVScroll.Width / 2;
            }
        }

        private void AdjustPanel2Controls()
        {
            int snapshotHeight = splitContainer1.Panel2.Height - lblChangeLeft.Height - checkboxLockVScroll.Height - (vertMargin * 2);

            rtbSnapshotLeft.Height = snapshotHeight - 50;
            rtbSnapshotRight.Height = snapshotHeight - 50;

            checkboxLockVScroll.Top = rtbSnapshotLeft.Bottom + vertMargin;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            AdjustControls();
        }

        private void Form1_ClientSizeChanged(object sender, EventArgs e)
        {
            AdjustControls();
        }

        private void rtbSnapshotLeft_VScroll(object sender, EventArgs e)
        {
            if (checkboxLockVScroll.Checked && !VRightScrollInProgress)
            {
                VLeftScrollInProgress = true;

                int charPos = rtbSnapshotLeft.GetCharIndexFromPosition(new Point(0, 0));
                rtbSnapshotRight.Select(charPos, 1);
                rtbSnapshotRight.ScrollToCaret();

                VLeftScrollInProgress = false;
            }
        }

        private void rtbSnapshotRight_VScroll(object sender, EventArgs e)
        {
            var offset = rtbSnapshotRight.AutoScrollOffset;

            if (checkboxLockVScroll.Checked && !VLeftScrollInProgress)
            {
                VRightScrollInProgress = true;

                int charPos = rtbSnapshotRight.GetCharIndexFromPosition(new Point(0, 0));
                rtbSnapshotLeft.Select(charPos, 1);
                rtbSnapshotLeft.ScrollToCaret();

                VRightScrollInProgress = false;
            }
        }

        private void rtbSnapshotRight_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                checkboxLockVScroll.Checked = !checkboxLockVScroll.Checked;
            }
        }

        private void rtbSnapshotLeft_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                checkboxLockVScroll.Checked = !checkboxLockVScroll.Checked;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // save any UI customizations the user made
            string lv1ColumnWidths = string.Format("{0},{1},{2},{3},{4},{5}", 
                listView1.Columns[0].Width,
                listView1.Columns[1].Width,
                listView1.Columns[2].Width,
                listView1.Columns[3].Width,
                listView1.Columns[4].Width,
                listView1.Columns[5].Width);

            string lv2ColumnWidths = string.Format("{0},{1},{2},{3},{4},{5},{6}", 
                listView2.Columns[0].Width,
                listView2.Columns[1].Width,
                listView2.Columns[2].Width,
                listView2.Columns[3].Width,
                listView2.Columns[4].Width,
                listView2.Columns[5].Width,
                listView2.Columns[6].Width);

            userSettings.ListView1ColumnWidths = lv1ColumnWidths;
            userSettings.ListView2ColumnWidths = lv2ColumnWidths;
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            listView2.Height = splitContainer1.Panel1.Height - lblChangeCount.Height - vertMargin;

            AdjustPanel2Controls();
        }

        private void displayDateTimesAsUTCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            userSettings.ShowLocalTime = !displayDateTimesAsUTCToolStripMenuItem.Checked;

            listView1.Columns[3].Text = displayDateTimesAsUTCToolStripMenuItem.Checked ? UtcTimeColumnHeader : LocalTimeColumnHeader;
            foreach(ListViewItem lvi1 in listView1.Items)
            {
                // column3 is the timestamp
                DateTime dtToModify = DateTime.Parse(lvi1.SubItems[3].Text);
                lvi1.SubItems[3].Text = displayDateTimesAsUTCToolStripMenuItem.Checked ? dtToModify.ToUniversalTime().ToString() : dtToModify.ToLocalTime().ToString();
            }

            listView2.Columns[2].Text = displayDateTimesAsUTCToolStripMenuItem.Checked ? UtcTimeColumnHeader : LocalTimeColumnHeader;
            foreach (ListViewItem lvi2 in listView2.Items)
            {
                // column 2 is the timestamp
                DateTime dtToModify = DateTime.Parse(lvi2.SubItems[2].Text);
                lvi2.SubItems[2].Text = displayDateTimesAsUTCToolStripMenuItem.Checked ? dtToModify.ToUniversalTime().ToString() : dtToModify.ToLocalTime().ToString();
            }
        }

        private void loadSessionHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HistoricalDocument document = null;

            var openFileDialog = new OpenFileDialog()
            {
                Filter = sessionHistoryFileFilter
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
                    catch(JsonSerializationException)
                    {
                        MessageBox.Show("Could not deserialize the file to a\nHistorical Document.", "Load Document Error");
                    }
                }
            }

            if (document != null)
            {
                listView1.Items.Clear();
                InitViews();

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
                listView1.Items.Add(lvi);

                foreach (DocumentStateSnapshot snapshot in document.DocumentSnapshots)
                {
                    string hashKey = SnapshotCache.GetHashString(
                        document.SessionName,
                        document.Branch,
                        snapshot.Change);

                    string snapshotBody;

                    if (snapshot.Change != SessionHistory.MaxChangeValue)
                    {
                        if (!SnapshotCache.TryGetSnapshot(hashKey, out snapshotBody))
                        {
                            snapshotBody = snapshot.Body;
                            SnapshotCache.AddSnapshotToCache(hashKey, snapshot.Body);
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
                    listView2.Items.Add(lvi2);
                }

                lblDocCount.Text = "1 document [offline]";
                DisplayChangesInfo();

                ShowingOfflineSession = true;
                saveSessionHistoryToolStripMenuItem.Visible = false;
            }
        }

        private void listView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    SessionDocumentContextMenuStrip.Show(Cursor.Position);
                }
            }
        }

        // ColumnClick event handler.
        internal void listView1_ColumnClick(object o, ColumnClickEventArgs e)
        {
            // Set the ListViewItemSorter property to a new ListViewItemComparer 
            // object. Setting this property immediately sorts the 
            // ListView using the ListViewItemComparer object.            
            this.listView1.ListViewItemSorter = new ListViewItemComparer(e.Column, isLV1SortOrderDescending);
            isLV1SortOrderDescending = !isLV1SortOrderDescending;
        }

        // ColumnClick event handler.
        internal void listView2_ColumnClick(object o, ColumnClickEventArgs e)
        {
            // Set the ListViewItemSorter property to a new ListViewItemComparer 
            // object. Setting this property immediately sorts the 
            // ListView using the ListViewItemComparer object.            
            this.listView2.ListViewItemSorter = new ListViewItemComparer(e.Column, isLV2SortOrderDescending);
            isLV2SortOrderDescending = !isLV2SortOrderDescending;
        }

        // Implements the manual sorting of items by columns.
        class ListViewItemComparer : IComparer
        {
            private int col;
            private bool isDescending;

            public ListViewItemComparer()
            {
                col = 0;
            }

            public ListViewItemComparer(int column, bool isDescending)
            {
                this.col = column;
                this.isDescending = isDescending;
            }

            public int Compare(object x, object y)
            {
                long value1;
                bool isNumber = long.TryParse(((ListViewItem)x).SubItems[col].Text, out value1);

                if (!isNumber)
                {
                    if (isDescending)
                    {
                        return String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text);
                    }
                    else
                    {
                        return String.Compare(((ListViewItem)y).SubItems[col].Text, ((ListViewItem)x).SubItems[col].Text);
                    }
                }
                else
                {
                    long value2;

                    if (string.IsNullOrWhiteSpace(((ListViewItem)y).SubItems[col].Text))
                    {
                        value2 = 0;
                    }
                    else
                    {                        
                        if (!long.TryParse(((ListViewItem)y).SubItems[col].Text, out value2))
                        {
                            value2 = SessionHistory.MaxChangeValue;
                        }
                    }

                    if (value1 == value2)
                    {
                        return 0;
                    }

                    if (isDescending)
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

        private async void saveSessionHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count != 1)
            {
                return;
            }
            
            int selectedIndex = listView1.SelectedIndices[0];

            HistoricalDocument document = new HistoricalDocument()
            {
                SessionName = listView1.Items[selectedIndex].SubItems[0].Text,
                Branch = listView1.Items[selectedIndex].SubItems[1].Text,
                NumSnapshots = int.Parse(listView1.Items[selectedIndex].SubItems[2].Text),
                LastModified = listView1.Items[selectedIndex].SubItems[3].Text,
                IsExpired = bool.Parse(listView1.Items[selectedIndex].SubItems[4].Text),
                ActivityId = listView1.Items[selectedIndex].SubItems[5].Text,
            };

            string eToken = await Authentication.GetDevTokenSlientlyAsync(tbScid.Text, tbSandbox.Text);

            lblExplanation.Text = "Downloading change history for the selected session...";

            Tuple<SessionHistoryDocumentResponse, string> queryResponse = await QueryForDocSessionHistoryAsync(eToken, document.SessionName, document.Branch);

            if (queryResponse.Item2 != null)
            {
                downloadPanel.Hide();

                MessageBox.Show(string.Format("{0}\n\nThe server may have been busy.\nPlease try again.", queryResponse.Item2), "Error!");
                return;
            }

            string errMsg = null;

            if (queryResponse.Item1 != null)
            {
                foreach (var item in queryResponse.Item1.Results)
                {
                    Tuple<string, string> getSnapshotResponse = await QueryForDocSessionHistoryChangeAsync(document.SessionName, document.Branch, item.changeNumber);
                    string snapshotBody = getSnapshotResponse.Item1;
                    errMsg = getSnapshotResponse.Item2;

                    if (errMsg != null)
                    {
                        break;
                    }

                    DocumentStateSnapshot snapshot = new DocumentStateSnapshot()
                    {
                        Change = item.changeNumber,
                        ModifiedByXuids = item.changedBy,
                        Timestamp = item.timestamp,
                        TitleId = item.titleId,
                        ServiceId = item.serviceId,
                        CorrelationId = item.correlationId,
                        ChangeDetails = item.details,
                        Body = snapshotBody != null ? snapshotBody : string.Empty
                    };

                    document.DocumentSnapshots.Add(snapshot);
                }
            }

            downloadPanel.Hide();

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
                    Filter = sessionHistoryFileFilter,
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

        private void btnCancel_Click(object sender, EventArgs e)
        {
            QueryCancelled = true;
            downloadPanel.Hide();
        }

        private void downloadPanel_VisibleChanged(object sender, EventArgs e)
        {
            if (downloadPanel.Visible == true)
            {
                // center the panel to the application
                downloadPanel.Left = downloadPanel.Parent.Width / 2 - downloadPanel.Width / 2;
                downloadPanel.Top = downloadPanel.Parent.Height / 2 - downloadPanel.Height / 2;
            }
        }

        private void lblExplanation_TextChanged(object sender, EventArgs e)
        {
            downloadPanel.Show();
        }

        private async void SingInout_Click(object sender, EventArgs e)
        {
            try
            {
                this.btnSingInout.Enabled = false;
                if (signedInuser != null)
                {
                    Authentication.SignOut();
                    signedInuser = null;
                }
                else
                {
                    DevAccountSource accountSource = DevAccountSource.WindowsDevCenter;
                    if (cmbAccountSource.SelectedIndex == 1)
                    {
                        accountSource = DevAccountSource.XboxDeveloperPortal;
                    }

                    this.signedInuser = await Authentication.SignInAsync(accountSource, null);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error");
            }
            finally
            {
                UpdateAccountPanel();
                this.btnSingInout.Enabled = true;
            }

        }

        private void cmbAccountSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            userSettings.AccountSource = cmbAccountSource.SelectedIndex;
        }

        private void UpdateAccountPanel()
        {
            this.signedInuser = Authentication.LoadLastSignedInUser();
            if (signedInuser != null)
            {
                btnSingInout.Text = "Sign Out";
                cmbAccountSource.Enabled = false;
                labelUserName.Text = signedInuser.Name;
                btnQuery.Enabled = true;
            }
            else
            {
                btnSingInout.Text = "Sign In";
                cmbAccountSource.Enabled = true;
                labelUserName.Text = "";
                btnQuery.Enabled = false;
            }
            
        }
    }
}
