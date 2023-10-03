using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName ="New Intro Card data", menuName ="AIStream/Intro Card/New Card")]
public class IntroCardData : ScriptableObject
{
    [Header("Loading Data")]
    public string noTopicsText = "Waiting for new topics";
    public string[] loadingTexts;
    public float loadingTextRefreshRate;
    public string progressText;

    [Header("Audio")]
    public AudioClip[] musicClips;

    [Header("Backgrounds")]
    public Sprite[] backgroundImages;
    public CardObject introCardContainer;
}
