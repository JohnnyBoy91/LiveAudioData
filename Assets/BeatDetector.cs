using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JCTools
{
    public class BeatDetector : MonoBehaviour
    {
        private AudioData audioData;
        public GameObject canvasObject;
        public GameObject spawnParent;
        public GameObject beatObjectPrefab;

        [System.Serializable]
        public class BeatData
        {
            public bool active = true;
            public bool automaticDetection = true;
            public int bandIndex = 0;
            public float beatThreshold = 1f;
            public bool beatSwitch = false;
        }

        public List<BeatData> beatData = new List<BeatData>();


        void Start()
        {
            audioData = GameObject.FindObjectOfType<AudioData>();
        }


        void Update()
        {
            for (int i = 0; i < beatData.Count; i++)
            {
                if (!beatData[i].active) break;

                if(beatData[i].automaticDetection)
                {
                    beatData[i].beatThreshold = (audioData.bandBuffer[beatData[i].bandIndex] + audioData.freqBandHighest[beatData[i].bandIndex]) / 2f;
                    //beatData[i].beatThreshold = ((audioData.bandBuffer[beatData[i].bandIndex] * 7f) + (audioData.freqBandHighest[beatData[i].bandIndex] * 3f)) / 10f;
                }

                if (!beatData[i].beatSwitch)
                {
                    if (audioData.freqBand[beatData[i].bandIndex] > beatData[i].beatThreshold)
                    {
                        beatData[i].beatSwitch = true;
                        TriggerBeat(i);
                    }
                }
                else
                {
                    if (audioData.freqBand[beatData[i].bandIndex] < beatData[i].beatThreshold)
                    {
                        beatData[i].beatSwitch = false;
                    }
                }
            }
        }

        private void TriggerBeat(int index)
        {
            GameObject newBeatObject = Instantiate(beatObjectPrefab, new Vector3(index * 2f, 5.6f, 0), Quaternion.identity, spawnParent.transform);
        }

    }
}