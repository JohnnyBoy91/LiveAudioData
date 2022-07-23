using UnityEngine;
using CSCore;
using CSCore.SoundIn;
using CSCore.DSP;
using CSCore.Streams;
using WinformsVisualization.Visualization;

namespace JCTools
{
    public class AudioData : MonoBehaviour
    {
        /// <summary>
        /// These control how the audio affects the buffers
        /// audio has very sharp changes in intensity and the buffer acts to soften those so values don't lerp too rapidly
        /// </summary>

        [Header("Audio Buffer Properties")]
        //rate at which the bands fade initially
        public float bufferDecreaseInitial = 0.015f;        //0.015f
        //increases the rate that bands fade over time
        public float bandFadeExponent = 1.2f;               //1.4f
        //fades the max band value
        public float bandHighestFade = 0.005f;              //0.015f
        //rate at which the band buffers increase
        public float bufferIncreaseRate = 0.01f;            //0.01f

        public Gradient globalGradient = new Gradient();

        //Amplifies buffer data
        public float bufferAmplifier = 1f;

        //Audio Bands, the spectrum data is spread across this many bands
        private int bandCount = 16;

        //Have your other scripts use these values to lerp properties such as size, position, color, gradient, shader properties etc
        public float[]  freqBand, bandBuffer, freqBandHighest;

        private float[] bufferDecrease, audioBand, audioBandBuffer;

        //Wasapi Stuff
        float[] fftBufferWasapi;
        LineSpectrum lineSpectrum;
        WasapiCapture capture;
        BasicSpectrumProvider spectrumProvider;
        IWaveSource finalSource;

        FftSize fftSize;
        float[] fftBuffer;
        public float[] barData;

        private int numBars = 7;

        [Header("Spectrum Data Properties")]
        public int minFreq = 0;
        public int maxFreq = 22000;
        public float highScaleAverage = 2.0f;
        public float highScaleNotAverage = 3.0f;
        public bool logScale = true;

        public bool init = false;
        private int initCounter = 0;

        public enum Channel { Stereo, Left, Right }
        public Channel channel = new Channel();

        //this stops everything from going completely dark.
        public float bandMinimum = 0.1f;

        void Awake()
        {
            Application.targetFrameRate = 60;
            audioBand = new float[bandCount];
            audioBandBuffer = new float[bandCount];
            freqBand = new float[bandCount];
            bandBuffer = new float[bandCount];
            bufferDecrease = new float[bandCount];
            freqBandHighest = new float[bandCount];

            numBars = bandCount;

            bufferAmplifier = 1f;
            SetupWasapiCapture();
        }

        private void Start()
        {
        }

        //Sets up wasapi capture; captures sound data playing on the pc
        private void SetupWasapiCapture()
        {
            capture = new WasapiLoopbackCapture();
            capture.Initialize();
            IWaveSource source = new SoundInSource(capture);
            // This is the typical size, you can change this for higher detail as needed
            fftSize = FftSize.Fft4096;
            // Actual fft data
            fftBuffer = new float[(int)fftSize];

            // These are the actual classes that give you spectrum data
            // The specific vars of lineSpectrum are changed below in the editor so most of these aren't that important here
            spectrumProvider = new BasicSpectrumProvider(capture.WaveFormat.Channels,
                        capture.WaveFormat.SampleRate, fftSize);

            lineSpectrum = new LineSpectrum(fftSize)
            {
                SpectrumProvider = spectrumProvider,
                UseAverage = true,
                BarCount = 7,
                BarSpacing = 2,
                IsXLogScale = false,
                ScalingStrategy = ScalingStrategy.Linear
            };

            // Tells us when data is available to send to our spectrum
            var notificationSource = new SingleBlockNotificationStream(source.ToSampleSource());
            notificationSource.SingleBlockRead += NotificationSource_SingleBlockRead;

            finalSource = notificationSource.ToWaveSource();
            capture.DataAvailable += Capture_DataAvailable;
            capture.Start();
        }

        private void OnApplicationQuit()
        {
            if (enabled)
            {
                capture.Stop();
                capture.Dispose();
            }
        }

        void Update()
        {
            GetSpectrumDataWasapi();
            RefreshAudioData();

            //inits after 2nd frame, depending on what you are capturing, you will need to wait for the audio buffers to update 1 frame to avoid NaNs
            if (!init)
            {
                initCounter++;
                if (initCounter == 2)
                {
                    init = true;
                }
            }
        }

        private void Capture_DataAvailable(object sender, DataAvailableEventArgs e)
        {
            finalSource.Read(e.Data, e.Offset, e.ByteCount);
        }

        private void NotificationSource_SingleBlockRead(object sender, SingleBlockReadEventArgs e)
        {
            spectrumProvider.Add(e.Left, e.Right);
        }

        private void GetSpectrumDataWasapi()
        {
            if (barData != null)
            {
                int numBars = barData.Length;
            }
            else
            {
                barData = new float[7];
            }

            float[] resData = GetWasapiFFT();

            if (resData == null)
            {
                return;
            }

            lock (barData)
            {
                for (int i = 0; i < numBars && i < resData.Length; i++)
                {
                    // Make the data between 0.0 and 1.0
                    //default divide by 100
                    barData[i] = resData[i] / 100.0f;
                }

                float average = 0;
                for (int a = 0; a < numBars && a < resData.Length; a++)
                {
                    average += barData[a];
                }
                average /= numBars;
                for (int i = 0; i < numBars && i < resData.Length; i++)
                {
                    if (lineSpectrum.UseAverage)
                    {
                        barData[i] = barData[i] + highScaleAverage * Mathf.Sqrt(i / (numBars + 0.0f)) * barData[i];
                    }
                    else
                    {
                        barData[i] = barData[i] + highScaleNotAverage * Mathf.Sqrt(i / (numBars + 0.0f)) * barData[i];
                    }
                    for (int k = 0; k < barData.Length; k++)
                    {
                        freqBand[k] = barData[k];
                        freqBand[k] *= bufferAmplifier * 3; //multiplier for spectrum data
                    }
                }
            }
        }

        private float[] GetWasapiFFT()
        {
            lock (barData)
            {
                lineSpectrum.BarCount = numBars;
                if (numBars != barData.Length)
                {
                    barData = new float[numBars];
                }
            }

            if (spectrumProvider.IsNewDataAvailable)
            {
                lineSpectrum.MinimumFrequency = minFreq;
                lineSpectrum.MaximumFrequency = maxFreq;
                lineSpectrum.IsXLogScale = logScale;
                lineSpectrum.SpectrumProvider.GetFftData(fftBuffer, this);
                return lineSpectrum.GetSpectrumPoints(100.0f, fftBuffer);
            }
            else
            {
                return null;
            }
        }

        private void RefreshAudioData()
        {

            for (int i = 0; i < bandCount; i++)
            {
                if (freqBand[i] > freqBandHighest[i])
                {
                    freqBandHighest[i] = freqBand[i];
                }
                else if (freqBand[i] > 0)
                {
                    freqBandHighest[i] -= bandHighestFade;
                }
                audioBand[i] = (freqBand[i] / freqBandHighest[i]);
                audioBandBuffer[i] = (bandBuffer[i] / freqBandHighest[i]);
            }

            for (int i = 0; i < bandBuffer.Length; i++)
            {
                //raise the bandbuffer, adding increase rate to slow down brightness gain
                if (freqBand[i] > bandBuffer[i])
                {
                    //bandBuffer[i] = freqBand[i];
                    bandBuffer[i] += Mathf.Clamp((freqBand[i] - bandBuffer[i]), 0, bufferIncreaseRate);
                    //bufferIncreaseRate;
                    bufferDecrease[i] = bufferDecreaseInitial;
                }
                //decrease the band buffer, using intermediary value to limit flashing
                if (freqBand[i] < bandBuffer[i])
                {
                    bandBuffer[i] -= bufferDecrease[i];
                    if (bandBuffer[i] < bandMinimum) bandBuffer[i] = bandMinimum;
                    bufferDecrease[i] *= bandFadeExponent;
                }
            }
        }
    }
}