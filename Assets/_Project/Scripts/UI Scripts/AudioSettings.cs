using UnityEngine;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;

    private const string PrefKey = "MasterVolume";

    void Start()
    {
        //load saved volume (default = 1)
        float v = PlayerPrefs.GetFloat(PrefKey, 1f);

        //set slider + apply immediately
        volumeSlider.SetValueWithoutNotify(v);
        ApplyVolume(v);

        //listen for changes
        volumeSlider.onValueChanged.AddListener(ApplyVolume);
    }

    public void ApplyVolume(float value)
    {
        AudioListener.volume = value;          // 0..1
        PlayerPrefs.SetFloat(PrefKey, value);  // save
        PlayerPrefs.Save();
    }
}