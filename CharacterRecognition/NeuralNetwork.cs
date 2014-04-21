using System;
using System.IO;
using System.Collections.Generic;

namespace CharacterRecognition
{
    public class NeuralNetwork
    {
        private static Random rand = new Random(Environment.TickCount);

        private int numInput;
        private int numHidden;
        private int numOutput;

        private double[] inputs;

        private double[][] ihWeights; // input-hidden
        private double[] hBiases;
        private double[] hOutputs;

        private double[][] hoWeights; // hidden-output
        private double[] oBiases;

        private double[] outputs;

        // back-prop specific arrays (these could be local to UpdateWeights)
        private double[] oGrads; // output gradients for back-propagation
        private double[] hGrads; // hidden gradients for back-propagation

        // back-prop momentum specific arrays (necessary as class members)
        private double[][] ihPrevWeightsDelta;  // for momentum with back-propagation
        private double[] hPrevBiasesDelta;
        private double[][] hoPrevWeightsDelta;
        private double[] oPrevBiasesDelta;
        private char [] letters = new char[26]{'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 
           'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'};
        public NeuralNetwork(int numInput, int numHidden, int numOutput)
        {
            this.numInput = numInput;
            this.numHidden = numHidden;
            this.numOutput = numOutput;

            this.inputs = new double[numInput];

            this.ihWeights = MakeMatrix(numInput, numHidden);
            this.hBiases = new double[numHidden];
            this.hOutputs = new double[numHidden];

            this.hoWeights = MakeMatrix(numHidden, numOutput);
            this.oBiases = new double[numOutput];

            this.outputs = new double[numOutput];

            // back-prop related arrays below
            this.hGrads = new double[numHidden];
            this.oGrads = new double[numOutput];

            this.ihPrevWeightsDelta = MakeMatrix(numInput, numHidden);
            this.hPrevBiasesDelta = new double[numHidden];
            this.hoPrevWeightsDelta = MakeMatrix(numHidden, numOutput);
            this.oPrevBiasesDelta = new double[numOutput];
            InitializeWeights();
        } // ctor

        private static double[][] MakeMatrix(int rows, int cols) // helper for ctor
        {
            double[][] result = new double[rows][];
            for (int r = 0; r < result.Length; ++r)
                result[r] = new double[cols];
            return result;
        }


        private void SetWeights(double[] weights)
        {
            // copy weights and biases in weights[] array to i-h weights, i-h biases, h-o weights, h-o biases
            int numWeights = (numInput * numHidden) + (numHidden * numOutput) + numHidden + numOutput;
            if (weights.Length != numWeights)
                throw new Exception("Bad weights array length: ");

            int k = 0; // points into weights param

            for (int i = 0; i < numInput; ++i)
                for (int j = 0; j < numHidden; ++j)
                    ihWeights[i][j] = weights[k++];
            for (int i = 0; i < numHidden; ++i)
                hBiases[i] = weights[k++];
            for (int i = 0; i < numHidden; ++i)
                for (int j = 0; j < numOutput; ++j)
                    hoWeights[i][j] = weights[k++];
            for (int i = 0; i < numOutput; ++i)
                oBiases[i] = weights[k++];
        }

        private void InitializeWeights()
        {
            // initialize weights and biases to small random values
            int numWeights = (numInput * numHidden) + (numHidden * numOutput) + numHidden + numOutput;
            double[] initialWeights = new double[numWeights];
            for (int i = 0; i < initialWeights.Length; ++i)
            {
                initialWeights[i] = 0.01 * rand.NextDouble() - 0.01;       //initial weights in [-0.01, 0.01]
            }
            this.SetWeights(initialWeights);
        }

        private double[] GetWeights()
        {
            // returns the current set of wweights, presumably after training
            int numWeights = (numInput * numHidden) + (numHidden * numOutput) + numHidden + numOutput;
            double[] result = new double[numWeights];
            int k = 0;
            for (int i = 0; i < ihWeights.Length; ++i)
                for (int j = 0; j < ihWeights[0].Length; ++j)
                    result[k++] = ihWeights[i][j];
            for (int i = 0; i < hBiases.Length; ++i)
                result[k++] = hBiases[i];
            for (int i = 0; i < hoWeights.Length; ++i)
                for (int j = 0; j < hoWeights[0].Length; ++j)
                    result[k++] = hoWeights[i][j];
            for (int i = 0; i < oBiases.Length; ++i)
                result[k++] = oBiases[i];
            return result;
        }

        // ----------------------------------------------------------------------------------------

        private double[] ComputeOutputs(List<double> xValues)
        {
            if (xValues.Count != numInput)
                throw new Exception("Bad xValues array length");

            double[] hSums = new double[numHidden]; // hidden nodes sums scratch array
            double[] oSums = new double[numOutput]; // output nodes sums

            for (int i = 0; i < xValues.Count; ++i) // copy x-values to inputs
                this.inputs[i] = xValues[i];

            for (int j = 0; j < numHidden; ++j)  // compute i-h sum of weights * inputs
                for (int i = 0; i < numInput; ++i)
                    hSums[j] += this.inputs[i] * this.ihWeights[i][j];

            for (int i = 0; i < numHidden; ++i)  // add biases to input-to-hidden sums
                hSums[i] += this.hBiases[i];

            for (int i = 0; i < numHidden; ++i)   // apply activation
                this.hOutputs[i] = SigmoidFunction(hSums[i]);

            for (int j = 0; j < numOutput; ++j)   // compute h-o sum of weights * hOutputs
                for (int i = 0; i < numHidden; ++i)
                    oSums[j] += hOutputs[i] * hoWeights[i][j];

            for (int i = 0; i < numOutput; ++i)  // add biases to input-to-hidden sums
                oSums[i] += oBiases[i];


            for (int i = 0; i < numOutput; ++i)// apply activation
            {
                double output = SigmoidFunction(oSums[i]);
                //if (output > 0.7)
                //    this.outputs[i] = 1;
                //else if (output < 0.1)
                //    this.outputs[i] = 0;
                //else
                    this.outputs[i] = output;
            }
          
         
            double[] retResult = new double[numOutput]; // could define a GetOutputs method instead
            Array.Copy(this.outputs, retResult, retResult.Length);
            return retResult;
        } // ComputeOutputs

        private static double SigmoidFunction(double x)
        {
            if (x < -45.0) return 0.0;
            else if (x > 45.0) return 1.0;
            else return 1.0 / (1.0 + Math.Exp(-x));
        }
        private static double HyperTanFunction(double x)
        {
            if (x < -20.0) return -1.0; // approximation is correct to 30 decimals
            else if (x > 20.0) return 1.0;
            else return Math.Tanh(x);
        }


        // ----------------------------------------------------------------------------------------

        private void UpdateWeights(double[] tValues, double learnRate, double momentum)
        {
            // update the weights and biases using back-propagation, with target values, eta (learning rate), alpha (momentum)
            // assumes that SetWeights and ComputeOutputs have been called and so all the internal arrays and matrices have values (other than 0.0)
            if (tValues.Length != numOutput)
                throw new Exception("target values not same Length as output in UpdateWeights");

            // 1. compute output gradients
            for (int i = 0; i < oGrads.Length; ++i)
            {
                double derivative = (1 - outputs[i]) * outputs[i];          //derivative of sigmoid
                // double derivative = (1 - outputs[i]) * (1 + outputs[i]); // derivative of tanh
                oGrads[i] = derivative * (tValues[i] - outputs[i]);
            }

            // 2. compute hidden gradients
            for (int i = 0; i < hGrads.Length; ++i)
            {
                double derivative = (1 - hOutputs[i]) * hOutputs[i];          //derivative of sigmoid
                // double derivative = (1 - hOutputs[i]) * (1 + hOutputs[i]); // derivative of tanh = (1 - y) * (1 + y)
                double sum = 0.0;
                for (int j = 0; j < numOutput; ++j) // each hidden delta is the sum of numOutput terms
                {
                    double x = oGrads[j] * hoWeights[i][j];
                    sum += x;
                }
                hGrads[i] = derivative * sum;
            }

            // 3a. update hidden weights (gradients must be computed right-to-left but weights can be updated in any order)
            for (int i = 0; i < ihWeights.Length; ++i) // 0..2 (3)
            {
                for (int j = 0; j < ihWeights[0].Length; ++j) // 0..3 (4)
                {
                    double delta = learnRate * hGrads[j] * inputs[i]; // compute the new delta
                    ihWeights[i][j] += delta; // update. note we use '+' instead of '-'. this can be very tricky.
                    ihWeights[i][j] += momentum * ihPrevWeightsDelta[i][j]; // add momentum using previous delta. on first pass old value will be 0.0 but that's OK.
                    ihPrevWeightsDelta[i][j] = delta;
                }
            }

            // 3b. update hidden biases
            for (int i = 0; i < hBiases.Length; ++i)
            {
                double delta = learnRate * hGrads[i] * 1.0; // the 1.0 is the constant input for any bias; could leave out
                hBiases[i] += delta;
                hBiases[i] += momentum * hPrevBiasesDelta[i]; // momentum
                hPrevBiasesDelta[i] = delta; // don't forget to save the delta
            }

            // 4. update hidden-output weights
            for (int i = 0; i < hoWeights.Length; ++i)
            {
                for (int j = 0; j < hoWeights[0].Length; ++j)
                {
                    double delta = learnRate * oGrads[j] * hOutputs[i];  // see above: hOutputs are inputs to the nn outputs
                    hoWeights[i][j] += delta;
                    hoWeights[i][j] += momentum * hoPrevWeightsDelta[i][j]; // momentum
                    hoPrevWeightsDelta[i][j] = delta; // save
                }
            }

            // 4b. update output biases
            for (int i = 0; i < oBiases.Length; ++i)
            {
                double delta = learnRate * oGrads[i] * 1.0;
                oBiases[i] += delta;
                oBiases[i] += momentum * oPrevBiasesDelta[i]; // momentum
                oPrevBiasesDelta[i] = delta; // save
            }
        } // UpdateWeights

        // ----------------------------------------------------------------------------------------

        public void Train(List<List<double>> dataset, int maxEprochs, double learnRate, double momentum)
        {
            int epoch = 0;
            double mse = 99;
            double[] xValues = new double[numInput]; // inputs
            double[] tValues = new double[numOutput]; // target values
            while (epoch < maxEprochs && mse > 0.001)
            {

                mse = MeanSquaredError(dataset);
  
                for (int i = 0; i < dataset.Count; ++i)
                {
                 //   tValues = Noise(tValues);
                    ComputeOutputs(dataset[i]); // copy xValues in, compute outputs (and store them internally)
                    tValues = GetTargetOutput(i);
                    UpdateWeights(tValues, learnRate, momentum); // use curr outputs and targets and back-prop to find better weights
                } // each training tuple
                ++epoch;
            }
        } // Train

        private double MeanSquaredError(List<List<double>> trainData) // used as a training stopping condition
        {
            // average squared error per training tuple
            double sumSquaredError = 0.0;
            double[] xValues = new double[numInput]; // first numInput values in trainData
            double[] tValues = new double[numOutput]; // last numOutput values

            for (int i = 0; i < trainData.Count; ++i) // walk thru each training case. looks like (6.9 3.2 5.7 2.3) (0 0 1)  where the parens are not really there
            {
                
                tValues = GetTargetOutput(i); // get target values
                double[] yValues = this.ComputeOutputs(trainData[i]); // compute output using current weights
                for (int j = 0; j < numOutput; ++j)
                {
                    double err = tValues[j] - yValues[j];
                    sumSquaredError += err * err;
                }
            }

            return sumSquaredError / trainData.Count;
        }
        private double[] GetTargetOutput(int letter)
        {
            double[] output = new double[numOutput];
            output[letter] = 1;
            return output;
        }
        // ----------------------------------------------------------------------------------------
        public char[] Recognize(List<List<double>> testData)
        {
            double[] xValues = new double[numInput]; // inputs
            double[] tValues = new double[numOutput]; // targets
            double[] yValues; // computed Y
            char[] predictedChars = new char[testData.Count];
            for (int i = 0; i < testData.Count; ++i)
            {
                yValues = this.ComputeOutputs(testData[i]);
                predictedChars = guessedLetter(yValues);
            }
            return predictedChars;
        }
        public char[] guessedLetter(double[] output)
        {
            char high = '?';
            char low = '?';
            for(int i = 0; i < output.Length; i++){
                if(output[i] > 0.7){
                    high = letters[i];
                }
                else if(output[i] > 0.5)
                {
                    low = letters[i];
                }
            }
            return (new char[2]{high, low});
        }
        public double Accuracy(List<double>[] testData)
        {
            // percentage correct using winner-takes all
            int numCorrect = 0;
            int numWrong = 0;
            double[] xValues = new double[numInput]; // inputs
            double[] tValues = new double[numOutput]; // targets
            double[] yValues; // computed Y
            Console.WriteLine("Test Data Result");
            for (int i = 0; i < testData.Length; ++i)
            {
                tValues = GetTargetOutput(i);
                yValues = this.ComputeOutputs(testData[i]);
                if (yValues[0] * 450 >= tValues[0] * 450 * 0.95 && yValues[0] * 450 <= tValues[0] * 450 * 1.05 ||
                    yValues[0] * 450 <= tValues[0] * 450 * 0.95 && yValues[0] * 450 >= tValues[0] * 450 * 1.05)
                    ++numCorrect;
                else
                    ++numWrong;
            }
            return (numCorrect * 1.0) / (numCorrect + numWrong); // ugly 2 - check for divide by zero
        }

    } // NeuralNetwork

}
