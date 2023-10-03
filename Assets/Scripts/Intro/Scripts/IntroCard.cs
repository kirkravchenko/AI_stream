using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class IntroCard
{
    public string topicName;
    private int previousLoadingTextIndex;
    private int currentLoadingTextIndex;
    private CardObject cardObject;
    public string currentLoadingString;
    private string currentProgressText;

    [Header("Settings")]
    public IntroCardData data;
    public IntroCard(string topicName, IntroCardData settings)
    {
        this.topicName = topicName;
        this.data = settings;
        currentLoadingTextIndex = previousLoadingTextIndex = 0;

    }
    public void UpdateLoadingProgress(float progress)
    {
        if(progress == -1f)
        {
            //flag for musical episode
            currentProgressText = "Preparing song...";
        }
        else if(progress == -2f)
        {
            //flag for fakeyou errors
            currentProgressText = "Lags caused by huge fakeyou queue!";
        }
        else if(progress == 0)
        {
            currentProgressText = "";
        }
        else
        {
          progress = Mathf.Clamp01(progress);
          currentProgressText = (progress * 100).ToString("F0") + "%";
        }

   //     Debug.Log("UPDATING PROGRESS AS: " + progress);
        if(cardObject != null )
        {
            cardObject.progressText.text = currentProgressText;
        }
    }
    public void SetCardInstance(CardObject cardObject)
    {
        this.cardObject = cardObject;
    }
    public void UpdateLoadingText(string text)
    {
        currentLoadingString = text;
        if(cardObject != null)
        {
            cardObject.loadingText.text = text;
        }
    }
    public void UpdateTopicText(string customTopic = "")
    {
        if(customTopic != "")
        {
            topicName = customTopic;
        }
        if (cardObject != null)
        {
            cardObject.topicText.text = topicName;
        }
        else Debug.Log("Want to update topic text but cardObject is null!");
    }
    public void UpdateBackgroundImage(Sprite sprite)
    {
        if(cardObject != null)
        {
            cardObject.backgroundImage.sprite = sprite;
        }
    }
    public Sprite GetRandomBackgroundImage()
    {
        return data.backgroundImages[UnityEngine.Random.Range(0, data.backgroundImages.Length)];
    }

    public string GetNextLoadingText()
    {
        if (data.loadingTexts.Length > 2)
        {
            do
            {
                currentLoadingTextIndex = UnityEngine.Random.Range(0, data.loadingTexts.Length);
            }
            while (currentLoadingTextIndex == previousLoadingTextIndex);
        }
        else currentLoadingTextIndex = (currentLoadingTextIndex + 1) % data.loadingTexts.Length;

        previousLoadingTextIndex = currentLoadingTextIndex;

        return data.loadingTexts[currentLoadingTextIndex];

    }
    public AudioClip GetRandomMusicClip()
    {
        return data.musicClips[UnityEngine.Random.Range(0, data.musicClips.Length)];
    }
}
