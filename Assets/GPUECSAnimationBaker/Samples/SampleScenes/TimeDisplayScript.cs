using System.Collections;
using TMPro;
using UnityEngine;

namespace GPUECSAnimationBaker.Samples.SampleScenes
{
    public class TimeDisplayScript : MonoBehaviour
    {
        private IEnumerator Start()
        {
            TextMeshProUGUI text = GetComponent<TextMeshProUGUI>();
            while (true)
            {
                float t = Time.timeSinceLevelLoad;
                int minutes = (int)(t / 60f);
                int seconds = (int)(t % 60f);
                text.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}
