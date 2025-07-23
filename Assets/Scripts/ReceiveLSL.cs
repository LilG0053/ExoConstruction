using LSL;
using System.Collections;
using UnityEngine;
using XCharts.Runtime;

namespace LSL4Unity.Samples.SimpleInlet
{
    public class SimpleInletScaleObject : MonoBehaviour
    {
        public string StreamName;
        public LineChart chart;

        private ContinuousResolver resolver;

        private StreamInlet inlet;
        private float[] sample_buffer;
        private int n_channels;

        void Start()
        {
            if (!string.IsNullOrEmpty(StreamName))
            {
                Debug.Log($"LSL Stream- starting input for stream \"{StreamName}\"");
                resolver = new ContinuousResolver("name", StreamName);
            }
            else
            {
                this.enabled = false;
                return;
            }

            StartCoroutine(ResolveExpectedStream());
        }

        IEnumerator ResolveExpectedStream()
        {
            var results = resolver.results();

            while (results.Length == 0)
            {
                yield return new WaitForSeconds(0.1f);
                results = resolver.results();
            }

            Debug.Log($"LSL Stream- \"{StreamName}\" found.");
            inlet = new StreamInlet(results[0]);

            n_channels = inlet.info().channel_count();
            sample_buffer = new float[n_channels];

            Debug.Log($"LSL Stream- stream info: {n_channels} channels.");
        }

        void Update()
        {
            if (inlet != null)
            {
                double timestamp = inlet.pull_sample(sample_buffer, 0.0);

                if (timestamp != 0.0)
                {
                    if (n_channels >= 3)
                    {
                        float x = sample_buffer[0];
                        float y = sample_buffer[1];
                        float z = sample_buffer[2];

                        chart.AddData(0, x);

                        Vector3 new_scale = new Vector3(x, y, z);
                        gameObject.transform.localScale = new_scale;

                        Debug.Log($"LSL Stream- updated object scale to: {new_scale}");
                    }
                    else
                    {
                        Debug.LogWarning($"LSL Stream- not enough channels- found {n_channels}, need >= 3");
                    }
                }
            }
            else
            {
                Debug.LogWarning("LSL Stream- no inlet connected yet");
            }
        }
    }
}
