using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class IntroController : MonoBehaviour
{
    [SerializeField] IntroCardData[] cardDatas;
    [SerializeField] AudioSource audioSource;
    private IntroCard introCard = null;
    private GameObject generatedIntroCard;
    private string currentTopicName;

    private static IntroController instance;
    public static IntroController Instance {  get { return instance; } }

    private bool stopLoadingTextCoroutine = false;
    //For testing purposes
    [ContextMenu("FORCE DISPLAY INTRO")]
    public void ForceStartIntro()
    {
        if (generatedIntroCard != null)
            HideIntro();
        Display("i mean.... you forced me to display intro so here you go");
    }

    [ContextMenu("FORCE STOP CURRENT INTRO")]
    public void ForceStopIntro()
    {
        HideIntro();
    }
    //
    private bool hidden = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }


        Display();
    }
    
    private void Start()
    {
        AIThing.OnTopicSelected += Display;
        AIThing.OnSceneReload += SceneReloaded;
        AIThing.OnEpisodeStart += HideIntro;
        AIThing.OnDialogueLineFullyGenerated += UpdateProgress;
        if(audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if(audioSource == null ) 
            { 
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

    }
    public void IntroNarrator(string redactedTopic = "")
    {
        StopMusic();
        stopLoadingTextCoroutine = true;
        StopCoroutine(LoadingTextsSwitchCoroutine());
        if(generatedIntroCard != null && introCard != null)
        {
            introCard.UpdateLoadingText("");
            introCard.UpdateLoadingProgress(0);
            if(redactedTopic != "")
            {
                introCard.UpdateTopicText(redactedTopic);
            }
        }
    }
    private void UpdateProgress(float progress)
    {
        if (generatedIntroCard == null) return;
        introCard.UpdateLoadingProgress(progress);
    }
    private void SceneReloaded()
    {
        if (generatedIntroCard != null) return;

        Display();
    }
    private void OnDestroy()
    {
        AIThing.OnTopicSelected -= Display;
        AIThing.OnSceneReload -= SceneReloaded;
        AIThing.OnEpisodeStart -= HideIntro;
        AIThing.OnDialogueLineFullyGenerated -= UpdateProgress;
    }
    public void Display(string topicName = "")
    {
        if (topicName == currentTopicName && generatedIntroCard != null)
        {
               Hide(false,true);
        }
        else
        {
            currentTopicName = topicName;
            Hide(false);
            CreateIntroCard(currentTopicName);
            hidden = false;
            stopLoadingTextCoroutine = false;
            StartCoroutine(LoadingTextsSwitchCoroutine());
            PlayMusic();
        }
    }

    private string LoadCurrentTopic()
    {
        string currentTopicPath = "Assets/Scripts/currentTopic.txt";
        if (File.Exists(currentTopicPath)) return File.ReadAllText(currentTopicPath);
        return null;
    }
    private IEnumerator LoadingTextsSwitchCoroutine()
    {
        float currentLoadingTextTime = introCard.data.loadingTextRefreshRate;
        if(introCard == null)
        {
            Debug.LogError("Intro card is null, bruh!");
            yield return null;
        }
        if(generatedIntroCard != null)
        {
            while (generatedIntroCard != null && generatedIntroCard.activeSelf && !stopLoadingTextCoroutine) //while intro card is active, keep updating loadingTexts
            {
                currentLoadingTextTime -= Time.deltaTime;
                if (currentLoadingTextTime < 0)
                {
                    string newloadingText = introCard.GetNextLoadingText();
                    introCard.UpdateLoadingText(newloadingText);
                    currentLoadingTextTime = introCard.data.loadingTextRefreshRate;
                }
                yield return null;
            }
        }


    }
    private void Hide(bool stopMusic = true,bool episodeStarted = false)
    {
        if (episodeStarted) return;

        if (generatedIntroCard == null)
        {
          //  Debug.Log("There's no intro card to hide!");
            return;
        }
        if(stopMusic)
           StopMusic();

        CardObject[] cardObject = FindObjectsOfType<CardObject>();
        if(cardObject.Length > 0)
        {
            foreach (CardObject card in cardObject)
            {
                Destroy(card.gameObject);
            }
        }
        generatedIntroCard = null;

    }
    private void PlayMusic()
    {
        if (introCard == null || audioSource == null || !audioSource.enabled) return;
        if (audioSource.isPlaying) return;

        if(audioSource.clip == null)
            audioSource.clip = introCard.GetRandomMusicClip();
        audioSource.loop = true;
        audioSource.Play();
    }
    private void StopMusic()
    {
        if(audioSource == null) return;
        audioSource.Stop(); 
    }
    public void HideIntro()
    {
        StopMusic();

        CardObject[] cardObject = FindObjectsOfType<CardObject>();
        if (cardObject.Length > 0)
        {
            foreach (CardObject card in cardObject)
            {
                Destroy(card.gameObject);
            }
        }
        generatedIntroCard = null;
        hidden = true;
    }

    public void CreateIntroCard(string topicName)
    {

        introCard ??= NewIntroCard(topicName ?? "Narrator forgot the actual topic again...");
        if (introCard != null)
        {
            if(introCard.data == null)
                introCard.data = cardDatas[UnityEngine.Random.Range(0, cardDatas.Length)];

            generatedIntroCard = Instantiate(introCard.data.introCardContainer.gameObject) as GameObject;
            generatedIntroCard.transform.SetParent(transform, true);


            generatedIntroCard.SetActive(true);

            audioSource.clip = introCard.GetRandomMusicClip();

            introCard.SetCardInstance(generatedIntroCard.GetComponent<CardObject>());

            string newloadingText = introCard.GetNextLoadingText();
            introCard.UpdateLoadingText(newloadingText);

            Sprite newBackgroundImage = introCard.GetRandomBackgroundImage();
            introCard.UpdateBackgroundImage(newBackgroundImage);

            introCard.topicName = string.IsNullOrEmpty(topicName) ? introCard.topicName = introCard.data.noTopicsText : topicName;
            introCard.UpdateTopicText();
        }
        else Debug.LogError("Couldn't create new intro card!");
    }

    private IntroCard NewIntroCard(string topicName)
    {
        IntroCardData cardData = cardDatas[UnityEngine.Random.Range(0, cardDatas.Length)];
        IntroCard newCard = new IntroCard(topicName, cardData);
        return newCard;
    }
}
