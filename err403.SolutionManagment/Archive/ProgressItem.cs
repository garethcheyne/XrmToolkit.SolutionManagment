using err403.SolutionManagment.AppCode;
using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.IO;
using System.Windows.Forms;

namespace err403.SolutionManagment
{
    public partial class ProgressItem : UserControl
    {
        private ToolTip tp = new ToolTip();

        public ProgressItem()
        {
            InitializeComponent();

            pbProgress.Image = ilProgress.Images[0];
            pnlProgress.Visible = false;
        }

        public event EventHandler<DownloadLogEventArgs> LogFileRequested;
        public event EventHandler<DownloadLogEventArgs> ViewMessageRequested;

        public ConnectionDetail Detail { get; set; }
        public OrganizationRequest Request { get; set; }
        public string Solution { get; set; }
        public byte[] SolutionFile { get; set; }
        public string SolutionFileName => Request is ExportSolutionRequest esr ? $"{esr.SolutionName}_{SolutionVersion.Replace(".", "_")}{(esr.Managed ? "_managed" : "")}.zip" : "";
        public string SolutionVersion { get; set; }
        public string AsyncErrorMessage { get; set; }
        public Enumerations.RequestType Type { get; set; }

        public void CheckDependencies()
        {
            Invoke(new Action(() =>
            {
                pnlProgress.Visible = true;
                lblProgress.Text = string.Format(lblProgress.Tag.ToString(), DateTime.Now.ToString("G"));
                lblPercentage.Visible = false;
                lblAction.Text = "Dependencies check";
                lblDirection.Text = $@"On organization {Detail.ConnectionName}";
                pbProgress.Image = ilProgress.Images[5];
            }));
        }

        public void Error(DateTime date, string errorMessage = null)
        {
            Invoke(new Action(() =>
            {
                pbProgress.Image = ilProgress.Images[2];
                if (Request is ExportSolutionRequest)
                {
                    llDownloadLog.Text = @"See error message";
                    llDownloadLog.Tag = errorMessage;
                    llDownloadLog.Visible = true;
                }
                else if (errorMessage != null && Request == null)
                {
                    llDownloadLog.Text = @"See error message";
                    llDownloadLog.Tag = errorMessage;
                    llDownloadLog.Visible = true;
                }
                else
                {
                    llDownloadLog.Visible = true;
                    llViewMessage.Visible = Request is ImportSolutionRequest || Request is StageAndUpgradeRequest;
                }
                lblProgress.Text += $@" - {date:HH:mm:ss}";
            }));
        }

        public void PublishTimeout(DateTime now)
        {
            Invoke(new Action(() =>
            {
                pnlProgress.Visible = true;
                lblProgress.Text = string.Format("Timeout occured - {0}", now.ToString("G"));
                lblPercentage.Visible = false;
                pbProgress.Image = ilProgress.Images[6];
                tp.SetToolTip(pbProgress, "A timeout occurs. This does not mean that the publish operation failed. Please check the publish operation status in Solution history in Power Apps Maker portal");
            }));
        }

        public void Skip(string reason)
        {
            Invoke(new Action(() =>
            {
                pnlProgress.Visible = true;
                lblProgress.Text = reason;
                lblPercentage.Visible = false;
                pbProgress.Image = ilProgress.Images[6];
                llDownloadLog.Visible = false;
            }));
        }

        public void Start()
        {
            Invoke(new Action(() =>
            {
                pnlProgress.Visible = true;
                lblProgress.Text = string.Format(lblProgress.Tag.ToString(), DateTime.Now.ToString("G"));
                pbProgress.Image = ilProgress.Images[1];
                lblPercentage.Visible = Request is ImportSolutionRequest || Request is StageAndUpgradeRequest;

                ProgressItem_Load(this, null);
            }));
        }

        public void Success(BaseToProcess p)
        {
            Invoke(new Action(() =>
            {
                // If error already set, we cannot succeed
                if (pbProgress.Image == ilProgress.Images[2]) return;

                pbProgress.Image = ilProgress.Images[3];
                llDownloadLog.Visible = Request is ImportSolutionRequest || Request is ExportSolutionRequest || Request is StageAndUpgradeRequest;
                llDownloadLog.Text = Request is ImportSolutionRequest || Request is StageAndUpgradeRequest ? "Download log file" : "Download solution";
                llViewMessage.Visible = Request is ImportSolutionRequest || Request is StageAndUpgradeRequest;
                lblProgress.Text = p.FinishedOn;
                lblPercentage.Visible = false;

                lblAction.Text = lblAction.Text.Replace("Upgrading", "Import");
            }));
        }

        public void Success(DateTime startedOn, DateTime completedOn)
        {
            Invoke(new Action(() =>
            {
                if (pbProgress.Image == ilProgress.Images[2]) return;

                pbProgress.Image = ilProgress.Images[3];
                llDownloadLog.Visible = false;
                var ts = completedOn - startedOn;
                lblProgress.Text = $"Completed in {(int)ts.TotalMinutes}m{ts.Seconds}s ({startedOn:hh:mm:ss} / {completedOn:hh:mm:ss})";
                lblPercentage.Visible = false;
            }));
        }

        internal void ReportProgress(double v, BaseToProcess p, bool isUpgrading = false)
        {
            Invoke(new Action(() =>
            {
                lblPercentage.Text = $@"{v:N0} %";
                lblProgress.Text = p.Ago;

                if (isUpgrading)
                {
                    if (lblAction.Text.IndexOf("Import") >= 0)
                    {
                        //lblProgress.Text += $@" - {DateTime.Now:HH:mm:ss}";
                        lblProgress.Text = p.FinishedOn;
                    }

                    lblAction.Text = lblAction.Text.Replace("Import", "Upgrading");
                    pbProgress.Image = ilProgress.Images[4];
                    lblPercentage.Visible = false;
                }
            }));
        }

        private void llDownloadLog_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (Request is ImportSolutionRequest isr)
            {
                LogFileRequested?.Invoke(this, new DownloadLogEventArgs
                {
                    ImportJobId = isr.ImportJobId,
                    Service = Detail.GetCrmServiceClient()
                });
            }
            else if (Request is StageAndUpgradeRequest saur)
            {
                LogFileRequested?.Invoke(this, new DownloadLogEventArgs
                {
                    ImportJobId = saur.ImportJobId,
                    Service = Detail.GetCrmServiceClient()
                });
            }
            else if (Request is ExportSolutionRequest esr)
            {
                if (((LinkLabel)sender).Tag != null)
                {
                    MessageBox.Show(this, ((LinkLabel)sender).Tag.ToString(), @"Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                using (var sfd = new SaveFileDialog
                {
                    Filter = @"Zip file (*.zip)|*.zip",
                    FileName = $"{esr.SolutionName}_{SolutionVersion.Replace(".", "_")}{(esr.Managed ? "_managed" : "")}.zip"
                })
                {
                    if (sfd.ShowDialog(Parent) == DialogResult.OK)
                    {
                        File.WriteAllBytes(sfd.FileName, this.SolutionFile);
                        MessageBox.Show(Parent, $@"File saved to {sfd.FileName}", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void llViewMessage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (Request is ImportSolutionRequest isr)
            {
                ViewMessageRequested?.Invoke(this, new DownloadLogEventArgs
                {
                    ImportJobId = isr.ImportJobId,
                    Service = Detail.GetCrmServiceClient()
                });
            }
            else if (Request is StageAndUpgradeRequest saur)
            {
                ViewMessageRequested?.Invoke(this, new DownloadLogEventArgs
                {
                    ImportJobId = saur.ImportJobId,
                    Service = Detail.GetCrmServiceClient()
                });
            }
        }

        private void ProgressItem_Load(object sender, EventArgs e)
        {
            if (Type == Enumerations.RequestType.Publish)
            {
                lblAction.Text = "Publish customizations";
                lblDirection.Text = $@"On organization {Detail.ConnectionName}";
            }
            else if (Type == Enumerations.RequestType.Remove)
            {
                lblAction.Text = $@"Remove {Solution}";
                lblDirection.Text = $@"From organization {Detail.ConnectionName}";
            }
            else
            {
                lblAction.Text = $@"{(Type == Enumerations.RequestType.Export ? "Export" : "Import")} {Solution} {SolutionVersion}";
                lblDirection.Text = $@"{(Type == Enumerations.RequestType.Export ? "From" : "To")} organization {Detail.ConnectionName}";
            }
        }
    }
}