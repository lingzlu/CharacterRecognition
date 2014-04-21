#region Copyright (c), Some Rights Reserved
/*##########################################################################
 * 
 * RecognitionSystem.cs
 * -------------------------------------------------------------------------
 * By
 * Murat FIRAT, June 2007
 * 
 * -------------------------------------------------------------------------
 * Description:
 * RecognitionSystem.cs implements the interface that uses backpropagation classes.
 * 
 * -------------------------------------------------------------------------
 * Notes:
 * To train the B.P.N. Network there must be a folder [in the same directory
 * of the .exe ] named "PATTERNS" which contains one image for each pattern.
 * (For example, [for english alfhabet] in "PATTERNS" directory 
 * there must be images, namely 0.bmp, 1.bmp, 2.bmp ... Z.bmp 
 * 
 * Sep. 2007:
 * I have removed some of drawing panel's features (scroll bars etc..) to 
 * make the app more understandable and simplified some other code.
 * 
 * -------------------------------------------------------------------------
 ###########################################################################*/
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections.Specialized;
using System.Configuration;
using System.Threading;

namespace CharacterRecognition
{
    public partial class RecognitionSystem : Form
    {
        //Neural Network Object With Output Type String
        private NeuralNetwork neuralNetwork = null;
        private List<List<double>>[] TrainingDatasets = null;
        private List<List<double>> TestingData = null;
        //Data Members Required For Neural Network
        private List<Dictionary<string, double[]>> TrainingSets = null;
        private int av_ImageHeight = 0;
        private int av_ImageWidth = 0;
        private int NumOfPatterns = 0;
        private int NumofSets = 28;
        //For Asynchronized Programming Instead of Handling Threads
        private delegate bool TrainingCallBack();
        private AsyncCallback asyCallBack = null;
        private IAsyncResult res = null;
        private ManualResetEvent ManualReset = null;

        private DateTime DTStart;

        public RecognitionSystem()
        {
            InitializeComponent();
            InitializeSettings();

         //   GenerateTrainingSets(NumofSets);
         //   CreateNeuralNetwork();
            
            asyCallBack = new AsyncCallback(TrainingCompleted);
            ManualReset = new ManualResetEvent(false);
        }
        
        //void neuralNetwork_IterationChanged(object o, NeuralEventArgs args)
        //{
        //    UpdateError(args.CurrentError);
        //    UpdateIteration(args.CurrentIteration);            

        //    if (ManualReset.WaitOne(0, true))
        //        args.Stop = true;
        //}     
           
        private void TrainingCompleted(IAsyncResult result)
        { 
            if(result.AsyncState is TrainingCallBack)
            {
                TrainingCallBack TR = (TrainingCallBack)result.AsyncState;

                bool isSuccess = TR.EndInvoke(res);
                if (isSuccess)
                    UpdateState("Completed Training Process Successfully\r\n");
                else
                    UpdateState("Training Process is Aborted or Exceed Maximum Iteration\r\n");
                                
                SetButtons(true);
                timer1.Stop();
            }
        }


        private void ShowRecognitionResults(string MatchedHigh, string MatchedLow, double OutputValueHight, double OutputValueLow)
        {
            labelMatchedHigh.Text = "Hight: " + MatchedHigh + " (%" + ((int)100 * OutputValueHight).ToString("##") + ")";
            labelMatchedLow.Text = "Low: " + MatchedLow + " (%" + ((int)100 * OutputValueLow).ToString("##") + ")";

            pictureBoxInput.Image = new Bitmap(drawingPanel1.ImageOnPanel,
                pictureBoxInput.Width, pictureBoxInput.Height);

            if (MatchedHigh != "?")
                pictureBoxMatchedHigh.Image = new Bitmap(new Bitmap(textBoxTrainingBrowse.Text + "\\dataset0\\" + MatchedHigh + ".bmp"),
                    pictureBoxMatchedHigh.Width, pictureBoxMatchedHigh.Height);

            if (MatchedLow != "?")
                pictureBoxMatchedLow.Image = new Bitmap(new Bitmap(textBoxTrainingBrowse.Text + "\\dataset0\\" + MatchedLow + ".bmp"),
                    pictureBoxMatchedLow.Width, pictureBoxMatchedLow.Height);
        }


        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog FD = new OpenFileDialog();
            FD.Filter = "Bitmap Image(*.bmp)|*.bmp";
            FD.InitialDirectory = textBoxTrainingBrowse.Text;

            if (FD.ShowDialog() == DialogResult.OK)
            {
                string FileName = FD.FileName;
                if (Path.GetExtension(FileName) == ".bmp")
                {
                    textBoxBrowse.Text = FileName;
                    drawingPanel1.ImageOnPanel = new Bitmap(
                        new Bitmap(FileName), drawingPanel1.Width, drawingPanel1.Height);
                }
            }
            FD.Dispose();
        }

        private void buttonTrainingBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog FD = new FolderBrowserDialog();
            FD.SelectedPath = textBoxTrainingBrowse.Text;

            if (FD.ShowDialog() == DialogResult.OK)
            {
                textBoxTrainingBrowse.Text = FD.SelectedPath;
            }
            FD.Dispose();
        }

        private void GenerateTrainingSets(int size)
        {
            textBoxState.AppendText("Generating Training Set..");
          //  TrainingDatasets = new List<List<double>>();
          //  Bitmap processedImage = null;
          ////  string[] dataFolders = Directory.GetDirectories(textBoxTrainingBrowse.Text);
        
         
          //  for (int i = 0; i < size; i++)
          //  {
          //      string[] dataFiles = Directory.GetFiles(textBoxTrainingBrowse.Text +"\\dataset"+ i, "*.bmp");
          //      List<double>[] datasetFeatures = new List<double>[26];
          //      int letterIdx = 0;
          //      foreach (string s in dataFiles)
          //      {
          //          Bitmap Temp = new Bitmap(s);
          //          processedImage = ImageProcessing.Preprocessing(Temp);
                 
          //          datasetFeatures[letterIdx++] = ImageProcessing.FeatureExtraction(processedImage);
          //          Temp.Dispose();
          //      }
          //      TrainingDatasets.Add(datasetFeatures);
          //  }
            textBoxState.AppendText("Done!\r\n");
        }

        private void buttonSaveSettings_Click(object sender, EventArgs e)
        {
            textBoxState.AppendText("Saving Settings..");

            string[] Images = Directory.GetFiles(textBoxTrainingBrowse.Text, "*.bmp");
            NumOfPatterns = Images.Length;

            av_ImageHeight = 0;
            av_ImageWidth = 0;

            foreach (string s in Images)
            {
                Bitmap Temp = new Bitmap(s);
                av_ImageHeight += Temp.Height;
                av_ImageWidth += Temp.Width;
                Temp.Dispose();
            }
            av_ImageHeight /= NumOfPatterns;
            av_ImageWidth /= NumOfPatterns;

            int networkInput = av_ImageHeight * av_ImageWidth;

            //textBoxInputUnit.Text = ((int)((double)(networkInput + NumOfPatterns) * .5)).ToString();
            //textBoxHiddenUnit.Text = ((int)((double)(networkInput + NumOfPatterns) * .3)).ToString();
            textBoxOutputUnit.Text = NumOfPatterns.ToString();


            btnRecognize.Enabled = false;
            buttonSave.Enabled = false;

            textBoxState.AppendText("Done!\r\n");

            GenerateTrainingSets(NumofSets);
            CreateNeuralNetwork();
        }

        private void InitializeSettings()
        {
            textBoxState.AppendText("Initializing Settings..");
            TrainingSets = new List<Dictionary<string, double[]>>();
            try
            {
                NameValueCollection AppSettings = ConfigurationManager.AppSettings;

                comboBoxLayers.SelectedIndex = (Int16.Parse(AppSettings["NumOfLayers"]) - 1);
                textBoxTrainingBrowse.Text = Path.GetFullPath(AppSettings["DataDirectory"]);
                textBoxMaxError.Text = AppSettings["MaxError"];

                string[] Images = Directory.GetFiles(textBoxTrainingBrowse.Text+"/dataset0", "*.bmp");
                NumOfPatterns = Images.Length;

                av_ImageHeight = 0;
                av_ImageWidth = 0;

                foreach (string s in Images)
                {
                    Bitmap Temp = new Bitmap(s);
                    av_ImageHeight += Temp.Height;
                    av_ImageWidth += Temp.Width;
                    Temp.Dispose();
                }
                av_ImageHeight /= NumOfPatterns;
                av_ImageWidth /= NumOfPatterns;

                int networkInput = av_ImageHeight * av_ImageWidth;

                textBoxInputUnit.Text = ((int)((double)(networkInput + NumOfPatterns) * .33)).ToString();
                textBoxHiddenUnit.Text = ((int)((double)(networkInput + NumOfPatterns) * .11)).ToString();
                textBoxOutputUnit.Text = NumOfPatterns.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Initializing Settings: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            textBoxState.AppendText("Done!\r\n");
        }

        private void CreateNeuralNetwork()
        {
            if (TrainingDatasets == null)
                throw new Exception("Unable to Create Neural Network As There is No Data to Train..");



            //if (comboBoxLayers.SelectedIndex == 0)
            //{

            //    neuralNetwork = new NeuralNetwork<string>
            //        (new BP1Layer<string>(av_ImageHeight * av_ImageWidth, NumOfPatterns), TrainingSets);

            //}
            //else if (comboBoxLayers.SelectedIndex == 1)
            //{
            //    int InputNum = Int16.Parse(textBoxInputUnit.Text);

            //    neuralNetwork = new NeuralNetwork<string>
            //        (new BP2Layer<string>(av_ImageHeight * av_ImageWidth, InputNum, NumOfPatterns), TrainingSets);

            //}
            //else if (comboBoxLayers.SelectedIndex == 2)
            //{
            //    int InputNum = Int16.Parse(textBoxInputUnit.Text);
            //    int HiddenNum = Int16.Parse(textBoxHiddenUnit.Text);

            //    neuralNetwork = new NeuralNetwork<string>
            //        (new BP3Layer<string>(av_ImageHeight * av_ImageWidth, InputNum, HiddenNum, NumOfPatterns), TrainingSets);

            //}

            //neuralNetwork.IterationChanged +=
            //    new NeuralNetwork<string>.IterationChangedCallBack(neuralNetwork_IterationChanged);

            //neuralNetwork.MaximumError = Double.Parse(textBoxMaxError.Text);
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            ManualReset.Set();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            TimeSpan TSElapsed = DateTime.Now.Subtract(DTStart);
            UpdateTimer(TSElapsed.Hours.ToString("D2") + ":" +
                TSElapsed.Minutes.ToString("D2") + ":" +
                TSElapsed.Seconds.ToString("D2"));
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            //SaveFileDialog FD = new SaveFileDialog();
            //FD.Filter = "Network File(*.net)|*.net";
            //if (FD.ShowDialog() == DialogResult.OK)
            //{
            //    neuralNetwork.SaveNetwork(FD.FileName);
            //}
            //FD.Dispose();
        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog FD = new OpenFileDialog();

            //FD.Filter = "Network File(*.net)|*.net";
            //FD.InitialDirectory = Application.StartupPath;
            //if (FD.ShowDialog() == DialogResult.OK)
            //{
            //    neuralNetwork.LoadNetwork(FD.FileName);
            //}
            //btnRecognize.Enabled = true;
            //buttonSave.Enabled = true;

            //FD.Dispose();
        }

        #region Methods To Invoke UI Components If Required
        private delegate void UpdateUI(object o);
        private void SetButtons(object o)
        {
            //if invoke is required for a control, sure, it is also required for others
            //then, it is not needed to check all controls
            if (btnStop.InvokeRequired)
            {
                btnStop.Invoke(new UpdateUI(SetButtons), o);
            }
            else
            {
                bool b = (bool)o;
                btnStop.Enabled = !b;
                btnRecognize.Enabled = b;
                btnTrain.Enabled = b;
                buttonLoad.Enabled = b;
                buttonSave.Enabled = b;
            }
        }
        private void UpdateError(object o)
        {
            if (labelError.InvokeRequired)
            {
                labelError.Invoke(new UpdateUI(UpdateError), o);
            }
            else
            {
                labelError.Text = "Error: " + ((double)o).ToString(".###");
            }
        }
        private void UpdateIteration(object o)
        {
            if (labelIteration.InvokeRequired)
            {
                labelIteration.Invoke(new UpdateUI(UpdateIteration), o);
            }
            else
            {
                labelIteration.Text = "Iteration: " + ((int)o).ToString();
            }
        }

        private void UpdateState(object o)
        {
            if (textBoxState.InvokeRequired)
            {
                textBoxState.Invoke(new UpdateUI(UpdateState), o);
            }
            else
            {
                textBoxState.AppendText((string)o);
            }
        }

        private void UpdateTimer(object o)
        {
            if (labelTimer.InvokeRequired)
            {
                labelTimer.Invoke(new UpdateUI(UpdateTimer), o);
            }
            else
            {
                labelTimer.Text = (string)o;
            }
        }
       
        #endregion

        #region RadioButton & CheckBox Event Handlers- Not Important
        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonBrowse.Checked)
            {
                textBoxBrowse.Enabled = true;
                buttonBrowse.Enabled = true;
                drawingPanel1.Enabled = false;
            }
            else
            {
                textBoxBrowse.Enabled = false;
                buttonBrowse.Enabled = false;
                drawingPanel1.Enabled = true;
            }
        }       

        private void comboBoxLayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxLayers.SelectedIndex == 0)
            {
                textBoxInputUnit.Enabled = false;
                textBoxHiddenUnit.Enabled = false;
            }
            else if (comboBoxLayers.SelectedIndex == 1)
            {
                textBoxInputUnit.Enabled = true;
                textBoxHiddenUnit.Enabled = false;
            }
            else if (comboBoxLayers.SelectedIndex == 2)
            {
                textBoxInputUnit.Enabled = true;
                textBoxHiddenUnit.Enabled = true;
            }
        }
        #endregion

        private void btnTrain_Click(object sender, EventArgs e)
        {
            UpdateState("Began Training Process..\r\n");
            int inputCount = TrainingDatasets[0][0].Count;
            neuralNetwork = new NeuralNetwork(inputCount, 100, 26);
            foreach (var trainingSet in TrainingDatasets)
            {
                neuralNetwork.Train(trainingSet, 100, 0.2, 0.9);

            }
            //SetButtons(false);
            //ManualReset.Reset();
        
            UpdateState("Completed Training Process Successfully\r\n");
            SetButtons(true);

            //TrainingCallBack TR = new TrainingCallBack(neuralNetwork.Train(TrainingDatasets[0], 10000, 0.02, 0.1));
          //  res = TR.BeginInvoke(asyCallBack, TR);
            //DTStart = DateTime.Now;
            //timer1.Start();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            drawingPanel1.Clear();
        }

        private void btnRecognize_Click(object sender, EventArgs e)
        {
            TestingData = new List<List<double>>();
            string MatchedHigh = "?", MatchedLow = "?";

            //double[] input = ImageProcessing.ToMatrix(drawingPanel1.ImageOnPanel,
            //    av_ImageHeight, av_ImageWidth);
            Bitmap drawImage = drawingPanel1.ImageOnPanel;
            Bitmap test = ImageProcessing.Preprocessing(drawImage);
            picSource.Image = drawImage;


            TestingData.Add(ImageProcessing.FeatureExtraction(test));
            char[] result = neuralNetwork.Recognize(TestingData);
            MatchedHigh = result[0].ToString();
            MatchedLow = result[1].ToString();

            if (MatchedHigh != "?")
                pictureBoxMatchedHigh.Image = new Bitmap(new Bitmap(textBoxTrainingBrowse.Text + "\\dataset0\\" + MatchedHigh + ".bmp"),
                    pictureBoxMatchedHigh.Width, pictureBoxMatchedHigh.Height);

            if (MatchedLow != "?")
                pictureBoxMatchedLow.Image = new Bitmap(new Bitmap(textBoxTrainingBrowse.Text + "\\dataset0\\" + MatchedLow + ".bmp"),
                    pictureBoxMatchedLow.Width, pictureBoxMatchedLow.Height);

        }

        private void btnProcessImg_Click(object sender, EventArgs e)
        {
            Bitmap drawImage = drawingPanel1.ImageOnPanel;

            Bitmap tmp = ImageProcessing.Preprocessing(drawImage);

            picSource.Image = new Bitmap(new Bitmap(tmp), picSource.Width, picSource.Height);
            tmp.Dispose();
        }

        private void btnLoadDatasets_Click(object sender, EventArgs e)
        {
            textBoxState.AppendText("Generating Training Set..");
            TrainingDatasets = new List<List<double>>[10];
            Bitmap processedImage = null;
            //  string[] dataFolders = Directory.GetDirectories(textBoxTrainingBrowse.Text);

            for (int i = 0; i < 10; i++)
            {
                string[] dataFiles = Directory.GetFiles(textBoxTrainingBrowse.Text + "\\dataset" + i, "*.bmp");
                List<List<double>> datasetFeatures = new List<List<double>>();
                foreach (string s in dataFiles)
                {
                    Bitmap Temp = new Bitmap(s);
                    processedImage = ImageProcessing.Preprocessing(Temp);

                    datasetFeatures.Add(ImageProcessing.FeatureExtraction(processedImage));
                    Temp.Dispose();
                }
                TrainingDatasets[i] = datasetFeatures;
            }
            textBoxState.AppendText("Done!\r\n");
        }

        private void btnGetRectangle_Click(object sender, EventArgs e)
        {
            Bitmap drawImage = drawingPanel1.ImageOnPanel;
            Bitmap rect = ImageProcessing.GetRectangles(drawImage);

            picSource.Image = new Bitmap(new Bitmap(rect), picSource.Width, picSource.Height);
            rect.Dispose();
        }
    }
}