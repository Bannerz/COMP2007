using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
     [Header("Audio")]
    public AudioSource clickSfxSource;   //audioSource for button click (not the music source)
    public AudioClip clickClip;
    
    public AudioSource musicSource;      //menu music AudioSource
    public float fadeDuration = 1.5f;

    [Header("Scene")]
    public int sceneToLoadIndex = 1;

    private bool isStarting = false;

    public void PlayGame()
    {
        if (isStarting) return;
        isStarting = true;

        StartCoroutine(ClickFadeAndLoad());
    }

    private IEnumerator ClickFadeAndLoad()
    {
        // 1) Play click sound
        if (clickSfxSource != null && clickClip != null)
            clickSfxSource.PlayOneShot(clickClip);

        // 2) Fade out music
        if (musicSource != null)
        {
            float startVolume = musicSource.volume;
            float t = 0f;

            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime; // ignores timescale changes
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
                yield return null;
            }

            musicSource.volume = 0f;
            musicSource.Stop();
        }

        // 3) Load scene
        SceneManager.LoadSceneAsync(sceneToLoadIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
