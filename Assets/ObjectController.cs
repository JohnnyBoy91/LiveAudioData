using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JCTools
{
    public class ObjectController : MonoBehaviour
    {
        public AudioData audioData;
        
        public GameObject debugBandRawObject;
        public GameObject debugBandBufferObject;
        public GameObject debugBandHighestObject;

        private List<RectTransform> debugBandRawRectsList = new List<RectTransform>();
        private List<RectTransform> debugBandBufferRectsList = new List<RectTransform>();
        private List<RectTransform> debugBandHighestRectsList = new List<RectTransform>();

        void Start()
        {
            if (audioData == null) audioData = GameObject.FindObjectOfType<AudioData>();

            for (int i = 0; i < debugBandRawObject.transform.childCount; i++)
            {
                debugBandRawRectsList.Add(debugBandRawObject.transform.GetChild(i).GetComponent<RectTransform>());
            }
            for (int i = 0; i < debugBandBufferObject.transform.childCount; i++)
            {
                debugBandBufferRectsList.Add(debugBandBufferObject.transform.GetChild(i).GetComponent<RectTransform>());
            }
            for (int i = 0; i < debugBandHighestObject.transform.childCount; i++)
            {
                debugBandHighestRectsList.Add(debugBandHighestObject.transform.GetChild(i).GetComponent<RectTransform>());
            }
        }

        void Update()
        {
            RefreshDebugPanel();
        }

        public void RefreshDebugPanel()
        {
            for (int i = 0; i < debugBandRawRectsList.Count; i++)
            {
                if(i < audioData.freqBand.Length)
                {
                    debugBandRawRectsList[i].sizeDelta = new Vector2(8, audioData.freqBand[i] * 20);
                    debugBandHighestRectsList[i].sizeDelta = new Vector2(8, audioData.freqBandHighest[i] * 20);
                    debugBandBufferRectsList[i].sizeDelta = new Vector2(8, audioData.bandBuffer[i] * 20);
                }
            }
        }
    }
}