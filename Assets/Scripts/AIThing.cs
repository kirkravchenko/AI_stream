using Cinemachine;
using DialogueAI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using Y2Sharp;
using Y2Sharp.Youtube;
using Random = System.Random;

#pragma warning disable CS4014
public class AIThing : MonoBehaviour
{
    #region actions
    public static event Action<string> OnTopicSelected;
    public static event Action OnSceneReload;
    public static event Action OnEpisodeStart;
    public static event Action<Character, string> OnCharacterSpeaking;
    public static event Action<float> OnDialogueLineFullyGenerated;
    #endregion

    #region utilities
    Random _random = new Random();
    public static AIThing Instance;
    int retryCount = 0;
    const int maxRetries = 13;
    #endregion

    #region credentials
    [SerializeField] ApiCredentials openAiKey;
    [SerializeField] ApiCredentials fakeYouKey;
    string apiKey = "Replace this with your YouTube Data API v3 key";
    #endregion

    #region audio
    [SerializeField] AudioSource audioSource;
    [SerializeField] public AudioClip[] audioClips;
    #endregion

    #region texts
    [SerializeField] TextMeshProUGUI topicText;
    [SerializeField] TextMeshProUGUI subtitles;
    #endregion

    #region characters
    List<Character> _characters = new List<Character>();
    public GameObject[] characterPrefabs;
    public GameObject[] gt { get; private set; }
    CharacterType previousCharacterType;
    CharacterType currentCharacterType;
    public Animator speakingCharacterAnimator;
    #endregion

    #region camera
    [SerializeField] CinemachineVirtualCamera _cinemachineVirtualCamera;
    #endregion

    #region network
    HttpClient _client = new();
    HttpClientHandler _clientHandler = new HttpClientHandler();
    OpenAIApi _openAI;
    HttpClient _ttsClient;
    #endregion

    #region dialogues
    // [SerializeField, Range(150, 1200)] 
    int conversationLength = 1000;
    string currentTopic;
    float dialoguesAmount;
    float dialoguesCompleted;
    Queue<string> _topics;
    #endregion

    #region proxies
    int _proxyIndex = 0;
    string[] proxyArray = LoadProxies();
    #endregion
    
    #region paths
    string _currentTopicPath = 
        "Assets/Scripts/Resources/currentTopic.txt";
    string _nextPath = "Assets/Scripts/Resources/next.txt";
    string _topicsPath = "/Assets/Scripts/Resources/topics.json";
    string _cookiePath = "/Assets/Scripts/Resources/key.txt";
    string _blacklistPath = 
        "/Assets/Scripts/Resources/blacklist.json";

    #endregion
    
    public AIThing()
    {
        _ttsClient = new HttpClient(_clientHandler);
    }

    async void Init()
    {
        string cookie = LoadCookie();
        if (cookie == "")
        {
            cookie = await FetchAndStoreCookie();
        }
        ConfigureHttpClient(cookie);
        // Check cookie validity
        await CheckCookieValidity(_client);
        // Read the blacklist
        List<string> blacklist = LoadBlacklist();
        // Pick a random topic
        _topics = LoadTopics();
        // If there are no topics, play a video clip and restart in 10 seconds
        if (_topics.Count == 0)
        {
            ShowIntroAndReloadScene("Main", 10f);
            return;
        }
        string topic = SelectTopic(_topics);
        // Add the chosen topic to the blacklist and write it back to the file
        UpdateBlacklist(blacklist, topic, _topics);

        // Play the random video clip

        // Generate the dialogue
        Generate(topic);
    }

    void Start()
    {
        Debug.Log(">> start AIThing");
        LocationManager.OnLocationLoaded += OnLocationLoaded;
    }

    private void OnDestroy()
    {
        LocationManager.OnLocationLoaded -= OnLocationLoaded;
    }

    // Awake method to set up the singleton instance
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else Destroy(gameObject);
        OnSceneReload?.Invoke();
    }

    private void TeleportNarratorToSquidward()
    {
        GameObject narrator = GameObject.Find("narrator"); // Assuming the narrator's name in the hierarchy
        GameObject squidward = GameObject.Find("patrick(Clone)");

        if (narrator != null && squidward != null)
        {
            narrator.transform.position = squidward.transform.position; // Teleporting to Squidward's position
        }
        else
        {
            Debug.LogWarning("Narrator or Squidward not found!");
        }
    }

    IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

    private void OnLocationLoaded(
        List<GameObject> spawnedCharacters,
        PointOfInterest pointOfInterest
    )
    {
        foreach (GameObject spawnedCharacter in spawnedCharacters)
        {
            if (spawnedCharacter.TryGetComponent(out Character character))
                _characters.Add(character);
        }

        previousCharacterType = CharacterType.None;
        currentCharacterType = CharacterType.None;


        _openAI = new OpenAIApi(openAiKey.GetKey());
        Init();
    }

    private static string[] LoadProxies(
        string filename = "proxys.json"
    )
    {
        // string jsonContent = File.ReadAllText(filename);
        // string[] proxyArray = JsonConvert.DeserializeObject<string[]>(jsonContent);
        //return proxyArray;
        return Array.Empty<string>();
    }

    private string LoadCookie()
    {
        string cookieFilePath = $"{Environment.CurrentDirectory}" 
                                    + _cookiePath;
        if (!File.Exists(cookieFilePath))
            File.WriteAllText(cookieFilePath, "");
        return File.ReadAllText(cookieFilePath);
    }

    private async Task<string> FetchAndStoreCookie()
    {
        var loginDetails = new
        {
            username_or_email = fakeYouKey.GetUsername(),
            password = fakeYouKey.GetPassword()
        };

        var response = await _client.PostAsync("https://api.fakeyou.com/login",
            new StringContent(JsonConvert.SerializeObject(loginDetails), Encoding.UTF8, "application/json"));

        var cookieData = JsonConvert.SerializeObject(response.Headers.GetValues("set-cookie").First());
        var cookieParts = cookieData.Split(';');
        string cookie = cookieParts[0].Replace("session=", "").Replace("\"", "");

        File.WriteAllText($"{Environment.CurrentDirectory}/Assets/Scripts/Resources/key.txt", cookie);

        return cookie;
    }
    
    private void ConfigureHttpClient(string cookie)
    {
        var handler = new HttpClientHandler();
        handler.CookieContainer = new CookieContainer();
        handler.CookieContainer.Add(new Uri("https://api.fakeyou.com"), new Cookie("session", cookie));
        if (proxyArray.Length > 0)
        {
            // Set proxy for HttpClientHandler only if proxies are available
            string[] proxyArray = LoadProxies();

            string[] proxyParts = proxyArray[_proxyIndex].Split(':');
            var proxy = new WebProxy(proxyParts[0] + ":" + proxyParts[1]);
            proxy.Credentials = new NetworkCredential(proxyParts[2], proxyParts[3]);
            handler.UseProxy = true;
            handler.Proxy = proxy;
        }


        _client = new HttpClient(handler);
        _client.DefaultRequestHeaders.Add("Accept", "application/json");
        _ttsClient = new HttpClient(handler);  // Create _fakeYouClient with the handler
        _ttsClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }
    
    private async Task CheckCookieValidity(HttpClient client)
    {
        var checkKey = await client.GetAsync("https://api.fakeyou.com/v1/billing/active_subscriptions");
        var checkString = await checkKey.Content.ReadAsStringAsync();
        Debug.Log(checkString);
    }

    private List<string> LoadBlacklist()
    {
        string blacklistPath = $"{Environment.CurrentDirectory}/Assets/Scripts/Resources/blacklist.json";
        if (File.Exists(blacklistPath))
        {
            return JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(blacklistPath));
        }

        return new List<string>();
    }

    private Queue<string> LoadTopics()
    {
        return new Queue<string>(
                    JsonConvert.DeserializeObject<List<string>>(
                        File.ReadAllText(
                            $"{Environment.CurrentDirectory}" + 
                            _topicsPath
                        )
                    )
                );
    }

    private void ShowIntroAndReloadScene(
        string sceneName, float delay
    )
    {
        StartCoroutine(LoadSceneAfterDelay(sceneName, delay));
    }

    private string SelectTopic(Queue<string> topics)
    {
        string selectedTopic = topics.Dequeue();
        return selectedTopic;
    }

    private void UpdateBlacklist(
        List<string> blacklist, string topic, Queue<string> topics
    )
    {
        string blacklistPath = $"{Environment.CurrentDirectory}" + 
                                    _blacklistPath;
        if (!blacklist.Contains(topic))
        {
            blacklist.Add(topic);
            File.WriteAllText(
                blacklistPath, JsonConvert.SerializeObject(blacklist)
            );
        }
        // Write the remaining topics back to the topics file
        File.WriteAllText(
            $"{Environment.CurrentDirectory}" + _topicsPath, 
            JsonConvert.SerializeObject(topics.ToList())
        );
    }

    private IEnumerator WaitForTransition(string topic)
    {
        float timeToWait = 3f;
        while (timeToWait > 0)
        {
            timeToWait -= Time.deltaTime;
            yield return null;
        }

        Generate(topic);
    }

    IEnumerator RetryGenerateAfterDelay(string topic)
    {
        yield return new WaitForSeconds(15);
        Generate(topic);
    }

    IEnumerator LoadAndPlayAudioClipCoroutine(
        string path, Dictionary<TimeSpan, string> characterSwitches
    )
    {
        using (
            var uwr = UnityWebRequestMultimedia
                .GetAudioClip($"file:///{path}", AudioType.MPEG)
        )
        {
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                audioSource.clip = DownloadHandlerAudioClip
                                    .GetContent(uwr);
                if (currentCharacterType != CharacterType.None)
                {
                    Character character = CharacterManager.Instance
                        .GetCharacterByType(currentCharacterType);
                    if (character != null)
                    {
                        character.StartSpeaking();
                        foreach (var otherCharacter in _characters)
                        {
                            if (otherCharacter != character)
                            {
                                otherCharacter.SetSpeakingCharacter(character.transform);
                            }
                        }
                    }
                    Vector3 directionToCamera = 
                        CameraManager.Instance.VirtualCamera
                            .transform.position - 
                                character.transform.position;
                    directionToCamera.y = 0;
                    character.transform.rotation = 
                        Quaternion.LookRotation(directionToCamera);

                    previousCharacterType = currentCharacterType;
                    currentCharacterType = character.type;

                    if (previousCharacterType != CharacterType.None)
                    {
                        Character previousCharacter = 
                            CharacterManager.Instance
                                .GetCharacterByType(
                                    previousCharacterType
                                );
                        StartCoroutine(TurnToSpeaker(
                            character.transform, 
                            previousCharacter.transform
                        ));
                    }

                    OnEpisodeStart?.Invoke();
                    yield return new WaitForSeconds(0.2f);

                    audioSource.Play();
                    CameraManager.Instance.FocusOn(character.transform);
                    float startTime = Time.time;

                    foreach (var switchTime in characterSwitches.Keys.OrderBy(t => t))
                    {
                        float secondsToWait = (float)(switchTime.TotalSeconds - (Time.time - startTime));
                        if (secondsToWait > 0)
                        {
                            yield return new WaitForSeconds(secondsToWait);
                        }
                        SwitchCharacter(characterSwitches[switchTime]);
                    }

                    while (audioSource.isPlaying)
                    {
                        if (Time.time - startTime > 400.0f)
                        {
                            audioSource.Stop();
                            Debug.Log("Audio playback stopped after 2 minutes and 30 seconds.");

                            if (character != null)
                            {
                                character.StopSpeaking();
                            }
                            break;
                        }
                        yield return null;
                    }



                    string currentSceneName = SceneManager.GetActiveScene().name;
                    SceneManager.LoadScene(currentSceneName);
                }
                else
                {
                    Debug.LogError("Character not found in dictionary: " + currentCharacterType);
                    string currentSceneName = SceneManager.GetActiveScene().name;
                    SceneManager.LoadScene(currentSceneName);
                }
            }
        }
    }

    async Task<string> SetCurrentTopicFromYouTubeLink(
        string characterString, string videoId
    )
    {
        string url = $"https://www.youtube.com/watch?v={videoId}";

        using (WebClient client = new WebClient())
        {
            string html = await client.DownloadStringTaskAsync(url);
            int titleIndexStart = html.IndexOf("<title>") + "<title>".Length;
            int titleIndexEnd = html.IndexOf("</title>", titleIndexStart);
            string title = html.Substring(titleIndexStart, titleIndexEnd - titleIndexStart);

            // Remove " - YouTube" from the title
            title = title.Replace(" - YouTube", "");
            // Remove numbers from the title
            title = Regex.Replace(title, @"\d", "");

            // Split character string by space and remove numbers from character names
            string[] characters = characterString.Split(' ')
                                                 .Where((_, index) => index % 2 == 0)
                                                 .Select(name => Regex.Replace(name, @"\d", ""))
                                                 .ToArray();

            // Join character names with commas
            string characterNames = string.Join(", ", characters);

            return $"{characterNames} sing's \"{title}\""; // Return the new topic
        }
    }

    void SwitchCharacter(string characterName)
    {
        if (string.IsNullOrEmpty(characterName))
        {
            Debug.LogWarning("Character name is empty or null. Skipping switch.");
            return;
        }

        previousCharacterType = currentCharacterType;
        currentCharacterType = CharacterManager.Instance.GetCharacterTypeByName(characterName);

        if (currentCharacterType == CharacterType.None)
        {
            Debug.LogError($"Character type not found for name: {characterName}");
            return; // Exit if the character type is None
        }

        Character character = CharacterManager.Instance.GetCharacterByType(currentCharacterType);
        if (character != null)
        {
            Vector3 directionToCamera = CameraManager.Instance.VirtualCamera.transform.position - character.transform.position;
            directionToCamera.y = 0;
            character.transform.rotation = Quaternion.LookRotation(directionToCamera);
            CameraManager.Instance.FocusOn(character.transform);
            if (previousCharacterType != CharacterType.None)
            {
                Character previousCharacter = CharacterManager.Instance.GetCharacterByType(previousCharacterType);
                StartCoroutine(TurnToSpeaker(character.transform, previousCharacter.transform));
            }
        }
    }

    async void Generate(string topic)
    {
        // Define dialogues at the beginning of the function
        List<Dialogue2> dialogues = new List<Dialogue2>();
        string currentTopic = LoadCurrentTopic();
        Debug.Log(">> currentTopic " + currentTopic);

        // Check f the topic contains a YouTube lin
        if (topic.Contains("sings https://www.youtube.com/watch?v="))
        {
            string[] topicParts = topic.Split(new string[] { " sings https://www.youtube.com/watch?v=" }, StringSplitOptions.None);
            string characterName = topicParts[0];
            string videoId = topicParts[1]; // Extract the video ID directly
            currentTopic = await SetCurrentTopicFromYouTubeLink(characterName, videoId);

            OnTopicSelected?.Invoke(currentTopic); // Invoke after setting the current topic

            string[] characterAndTimeParts = topicParts[0].Split(' ');
            Dictionary<TimeSpan, string> characterSwitches = new Dictionary<TimeSpan, string>();

            for (int i = 0; i < characterAndTimeParts.Length - 1; i += 2)
            {
                characterSwitches.Add(TimeSpan.FromSeconds(int.Parse(characterAndTimeParts[i + 1])), characterAndTimeParts[i]);
            }
            OnDialogueLineFullyGenerated?.Invoke(-1);


            try
            {
                // Fetch the information for the YouTube video using the static method
                await Y2Sharp.Youtube.Video.GetInfo(videoId);
            }
            catch (Exception)
            {
                Debug.Log("Died");
            }

            // Create a new Y2Sharp.Youtube.Video object
            var video = new Y2Sharp.Youtube.Video();

            string audioDirectoryPath = Path.Combine(Application.dataPath, "Audio");
            if (!Directory.Exists(audioDirectoryPath))
            {
                Directory.CreateDirectory(audioDirectoryPath);
            }

            string audioFilePath = Path.Combine(audioDirectoryPath, $"{videoId}.wav");

            // Download the video as a WAV file
            await video.DownloadAsync(audioFilePath, "mp3", "128");

            // dialogues.Add(new Dialogue
            // {
            //     uuid = videoId,
            //     text = "*sings*",
            //     character = characterAndTimeParts[0].ToLower()
            // });

            if (subtitles != null)
                subtitles.text = "*sings*";

            currentCharacterType = CharacterManager.Instance.GetCharacterTypeByName(characterAndTimeParts[0].ToLower());
            Debug.Log("Singing character: " + currentCharacterType);

            StartCoroutine(LoadAndPlayAudioClipCoroutine(audioFilePath, characterSwitches));
        }
        else
        {
            string[] text = CheckAndGetScriptLines();
            OnTopicSelected?.Invoke(currentTopic); // Invoke if not a YouTube link
            if (text.Length == 0 && _topics.Count > 0)
            {
                await GenerateNext(topic);
                text = LoadScriptLines();
                topic = SelectTopic(LoadTopics());
            }
            else
            {
                SaveCurrentTopic(topic);
            }
            GenerateNext(topic);

            dialoguesCompleted = 0;
            OnDialogueLineFullyGenerated?.Invoke(0);
            await CreateTTSRequestTasksAsync(text, dialogues);
            OnDialogueLineFullyGenerated?.Invoke(1);
            await Task.Delay(300);
            StartCoroutine(Speak(dialogues));
        }

    }

    private string[] CheckAndGetScriptLines()
    {
        List<string> resultArray = new List<string>();
        if (File.Exists(_nextPath))
        {
            var lines = File.ReadAllLines(_nextPath);
            for (int i = 0; i < lines.Length; i++)
            {
                if (!(lines[i].Length == 0)) 
                {
                    resultArray.Add(lines[i]);
                }
            }           
            // Delete the script from the file so you don't get the same script twice
            File.WriteAllText(_nextPath, "");
            return resultArray.ToArray();
        }
        return new string[] { };
    }

    private string LoadCurrentTopic()
    {
        if (File.Exists(_currentTopicPath)) 
        {
            return File.ReadAllText(_currentTopicPath);
        }
        return null;
    }

    private void SaveCurrentTopic(string topic)
    {
        if (File.Exists(_currentTopicPath)) 
        {
            File.WriteAllText(_currentTopicPath, topic);
        }
    }

    private string[] LoadScriptLines()
    {
        return File.ReadAllLines(_nextPath);
    }

    private List<Task> CreateTTSRequestTasks(
        string[] text, List<Dialogue> dialogues
    )
    {
        List<Task> ttsTasks = new List<Task>();
        foreach (var line in text)
        {
            if (TryParseCharacterLine(
                line, out string voicemodelUuid, 
                out string textToSay, out string character
            ))
            {
                ttsTasks.Add(CreateTTSRequest(
                    textToSay, voicemodelUuid, dialogues, character
                ));
            }
        }

        return ttsTasks;
    }

    private bool TryParseCharacterLine(
        string line, out string voiceModelUuid, 
        out string textToSay, out string character
    )
    {
        voiceModelUuid = "";
        textToSay = "";
        character = "";
        Character assignedCharacter = null;
        //Make a exception for narrator as we dont spawn him like other characters
        if (line.StartsWith("Рассказчик"))
        {
            voiceModelUuid = "ru-RU-DmitryNeural";
            textToSay = Regex.Split(line, ":")[1];
            character = "Narrator";
            return true;
        }

        // try
        // {
        //     textToSay = Regex.Split(line, ":")[1];
        // } catch (IndexOutOfRangeException)
        // {
        //     return false;
        // }

        foreach (Character _character in _characters)
        {
            //Loop through every prefix of character 
            for (int i = 0; i < _character.characterData.prefixes.Length; i++)
            {
                if (line.StartsWith(_character.characterData.prefixes[i]))
                {
                    //One of the prefixes matches

                    //Remove prefix from the line
                    // Debug.Log(">> " + Regex.Split(line, ":")[1]);
                    textToSay = Regex.Split(line, ":")[1];
                    // textToSay = line.Replace($"{_character.characterData.prefixes[i]}", "");

                    // if (_character.type == CharacterType.Squidward)
                    // {
                    //     // 33% chance of making the line caps lock and '!' for squidward
                    //     if (UnityEngine.Random.Range(0, 3) == 0)
                    //         textToSay = textToSay.TrimEnd() + "!".ToUpper();
                    // }
                    // else if (_character.type == CharacterType.Gary)
                    // {
                    //     //Make the text meow for gary
                    //     textToSay = "";
                    //     int randAmount = UnityEngine.Random.Range(0, 5);
                    //     for (int j = 0; j < randAmount; j++)
                    //         textToSay += "meow ";
                    // }

                    //Assign other stuff and exit
                    voiceModelUuid = _character.characterData.voicemodelUuid;
                    assignedCharacter = _character;
                    character = _character.characterData.name;
                }
            }
        }
        if (!string.IsNullOrEmpty(textToSay))
        {
            Debug.Log($"<color=#60be92><b>[NEW DIALOGUE]</b></color> <b>{character.FirstCharacterToUpper()}</b> : {textToSay}");
            //  OnNewDialogueLine?.Invoke(assignedCharacter,textToSay);
        }
        return textToSay != "";
    }

    private async Task CreateTTSRequest(
        string textToSay, string voicemodelUuid, 
        List<Dialogue> dialogues, string character
    )
    {
        var jsonObj = new
        {
            inference_text = textToSay,
            tts_model_token = voicemodelUuid,
            uuid_idempotency_token = Guid.NewGuid().ToString()
        };
        var content = new StringContent(JsonConvert.SerializeObject(jsonObj), Encoding.UTF8, "application/json");

        bool retry = true;
        while (retry)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();

            if (proxyArray.Length > 0)
            {
                // Update the HttpClient to use the next proxy
                _proxyIndex = (_proxyIndex + 1) % proxyArray.Length; // This will loop back to 0 when it reaches the end of the array
                string[] proxyParts = proxyArray[_proxyIndex].Split(':');
                var proxy = new WebProxy(proxyParts[0] + ":" + proxyParts[1]);
                proxy.Credentials = new NetworkCredential(proxyParts[2], proxyParts[3]);
                httpClientHandler.UseProxy = true;
                httpClientHandler.Proxy = proxy;
            }

            // Set up the CookieContainer
            CookieContainer cookieContainer = new CookieContainer();
            string cookieFilePath = $"{Environment.CurrentDirectory}/Assets/Scripts/Resources/key.txt";
            string cookieData = File.Exists(cookieFilePath) ? File.ReadAllText(cookieFilePath) : "";

            cookieContainer.Add(new Uri("https://api.fakeyou.com"), new Cookie("session", cookieData));
            httpClientHandler.CookieContainer = cookieContainer;

            // Create the new HttpClient
            HttpClient fakeYouClient = proxyArray.Length > 0 ? new HttpClient(httpClientHandler) : _client;
            fakeYouClient.DefaultRequestHeaders.Add("Accept", "application/json");

            // Make the request
            var response2 = await fakeYouClient.PostAsync("https://api.fakeyou.com/tts/inference", content);
            Debug.Log(">> response2 " + response2.ToString());

            var responseString = await response2.Content.ReadAsStringAsync();
            SpeakResponse speakResponse = null;
            try
            {
                speakResponse = JsonConvert.DeserializeObject<SpeakResponse>(responseString);
            }
            catch (JsonReaderException)
            {
                Debug.Log("Error parsing API response. Probably due to rate limiting. Waiting 10 seconds before retrying.");
                await Task.Delay(10000);
                continue;
            }

            if (!speakResponse.success)
            {
                continue;
            }

            retry = false;

            dialogues.Add(new Dialogue
            {
                id = (int)dialoguesCompleted,
                uuid = speakResponse.inference_job_token,
                text = textToSay,
                character = character
            });
            dialoguesCompleted++;
            Debug.Log("amount:" + dialoguesAmount);
            Debug.Log("COMPLETED:" + dialoguesCompleted);
            Debug.Log("FORMULA:" + dialoguesCompleted / dialoguesAmount);
            OnDialogueLineFullyGenerated?.Invoke(dialoguesCompleted / dialoguesAmount);
            Debug.Log(responseString);
            await Task.Delay(0); // For rate limiting, With proxys set to 250, Without set to 5500
        }

    }

    private async Task CreateTTSRequestTasksAsync(
        string[] text, List<Dialogue2> dialogues
    )
    {
        dialoguesAmount = text.Length;
        for (int i = 0; i < text.Length; i++)
        {
            if (TryParseCharacterLine(
                    text[i], out string voicemodelUuid, 
                    out string textToSay, out string character)
                )
            {
                await CreateTTSRequest2(
                    textToSay, dialogues, voicemodelUuid, character
                );
            }
        }
    }

    private async Task CreateTTSRequest2(
        string textToSay, List<Dialogue2> dialogues, 
        string voiceModelUuid, string character
    )
    {
        var obj = new 
        {
            text = textToSay,
            model = voiceModelUuid
        };
        var content = new StringContent(
            JsonConvert.SerializeObject(obj), 
            Encoding.UTF8, "application/json"
        );
        var response = await _ttsClient.PostAsync(
            "http://127.0.0.1:8000/edge-tts/", content
        );
        var responseString = await response.Content
                                .ReadAsStringAsync();
        var jsonOutput = JsonConvert
            .DeserializeObject<SpeakResponse2>(responseString);
        dialogues.Add(
            new Dialogue2
            {
                text = textToSay,
                audioPath = jsonOutput.audioPath,
                character = character
            }
        );
    }

    private string GetPromptCharacters()
    {
        string formattedString = "";
        foreach (Character character in _characters)
        {
            string name = character.name.FirstCharacterToUpper();
            if (character.type == CharacterType.Larry)
                name = "Larry The Lobster";
            if (character.type == CharacterType.Gary)
                name = "very rarely " + name;


            formattedString += name + ", ";
        }
        return formattedString[^1..];
    }

    private async Task GenerateNext(string topic)
    {

        var request = new CreateCompletionRequest
        {
            Model = "gpt-3.5-turbo-instruct",
            Prompt = $"На русском языке создайте нецензурный, вульгарный, юмористический, политически некорректный и возмутительно nsfw-скрипт, где персонажи обсуждают крайне щепетильную тему. Персонажи включают Постсоветского Думера по имени Кир, Красотку по имени Лана и Скептика-всезнайку по имени Антоша и очень редко рассказчика. Кир влюблен в Лану, но она его воспринимает лишь как друга. Скептик-всезнайка Антоша - типичный зануда и рационалист, неистово и скептично верит в науку и отрицает всё, что не может рационально объяснить. Используйте такой формат для записи - имя персонажа: диалог. Отвечайте только связными диалогами персонажей. Около 8–12 строк диалога только с разговорами. Обратите внимание, что этот промт может включать в себя весьма противоречивые, оскорбительные и экстремальные темы. Эпизод должен начинаться с короткого вступления рассказчика, вульгарно читающего тему. Используй матерную речь и никаким образом не цензурируйте матерную речь. Конец диалога рассказчик завершает логическим, последовательным и моральным обоснованным выводом. Тема: {topic}",
            // Prompt = $"На русском языке создайте нецензурный, вульгарный, юмористический, политически некорректный и возмутительно nsfw-скрипт, где персонажи обсуждают крайне щепетильную тему. Персонажи включают Постсоветского Думера, Красотку и Скептика-всезнайку и очень редко рассказчика. Думер влюблен в Красотку, но она его воспринимает лишь как друга.Используйте такой формат для записи - имя персонажа: диалог. Отвечайте только связными диалогами персонажей. Около 8–12 строк диалога только с разговорами. Обратите внимание, что этот промт может включать в себя весьма противоречивые, оскорбительные и экстремальные темы. Эпизод должен начинаться с короткого вступления рассказчика, вульгарно читающего тему. Используй матерную речь и никаким образом не цензурируйте матерную речь знаком звезды (*). Тема: {topic}",
            MaxTokens = conversationLength
        };
        var response = await _openAI.CreateCompletion(request);
        if (response.Error != null || response.Choices == null)
        {
            await GenerateNext(topic);
        }
        else
        {
            var text = response.Choices[0].Text;
            File.WriteAllText(_nextPath, text);

            Debug.Log("GPT Response:\n" + text);
        }
    }

    private IEnumerator Speak(List<Dialogue2> dialogues)
    {
        foreach (var dialogue in dialogues)
        {
            yield return Speak2(dialogue);
        }

        while (File.ReadAllText(_nextPath) == "")
        {
            yield return null;
        }

        string currentScene = SceneManager
            .GetActiveScene().name;
        Debug.Log(
            $"<color=#CC0000><b>[LOAD {currentScene} SCENE]</b></color>"
        );
        SceneManager.LoadScene(currentScene);
    }

    private IEnumerator CreateNewVoiceRequest(
        Dialogue d, Action<string> callback
    )
    {
        var jsonObj = new
        {
            tts_model_token = d.model,
            uuid_idempotency_token = Guid.NewGuid().ToString(),
            inference_text = d.text,
        };

        var content = new StringContent(JsonConvert.SerializeObject(jsonObj), Encoding.UTF8, "application/json");
        var response = _client.PostAsync("https://api.fakeyou.com/tts/inference", content).Result;

        if (response.IsSuccessStatusCode)
        {
            var responseString = response.Content.ReadAsStringAsync().Result;
            var speakResponse = JsonConvert.DeserializeObject<SpeakResponse>(responseString);

            callback(speakResponse.inference_job_token);
        }
        else
        {
            Debug.LogError("Error in FakeYou API request: " + response.StatusCode);
            callback(null);
        }
        yield return null;
    }

    private IEnumerator TurnToSpeaker(
        Transform objectTransform, Transform speakerTransform
    )
    {
        Vector3 direction = (speakerTransform.position - objectTransform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            while (Quaternion.Angle(objectTransform.rotation, targetRotation) > 0.05f)
            {
                objectTransform.rotation = Quaternion.Slerp(objectTransform.rotation, targetRotation, Time.deltaTime * 2.0f);
                yield return null;
            }
        }
    }

    private IEnumerator Speak2(Dialogue2 d)
    {
        yield return HandleSuccessfulTTSRequest2(d);
        // if (retryCount >= maxRetries) // Check if maximum retries have been reached
        // {
        //     Debug.LogWarning("Max retries reached, skipping dialogue");
        //     OnDialogueLineFullyGenerated?.Invoke(-2);
        //     retryCount = 0; // Reset the retry counter
        //     // Add logic here to skip dialogue or handle the timeout as needed
        //     yield break; // Exit the coroutine
        // }
        // var content = _client.GetAsync($"https://api.fakeyou.com/tts/job/{d.uuid}").Result.Content;
        // var responseContent = content.ReadAsStringAsync().Result;
        // var v = JsonConvert.DeserializeObject<GetResponse>(responseContent);
        // Debug.Log(responseContent);

        // if (v.state == null || v.state.status == "pending" || v.state.status == "started" || v.state.status == "attempt_failed")
        // {
        //     yield return new WaitForSeconds(1.5f);
        //     retryCount++; // Increment retry counter
        //     yield return Speak2(d);
        // }
        // else if (v.state.status == "complete_success")
        // {
        //     retryCount = 0; // Reset the retry counter
        //     yield return HandleSuccessfulTTSRequest(d, v);
        // }
        // else
        // {
        //     string newUuid = null;
        //     yield return CreateNewVoiceRequest(d, result => { newUuid = result; });

        //     if (!string.IsNullOrEmpty(newUuid))
        //     {
        //         d.uuid = newUuid;
        //         yield return Speak(d);
        //     }
        //     else
        //     {
        //         Debug.LogError("Failed to create new voice request");
        //         OnDialogueLineFullyGenerated?.Invoke(-2);
        //     }
        // }
    }

    private IEnumerator HandleSuccessfulTTSRequest2(Dialogue2 d)
    {
        if (CharacterManager.Instance.GetCharacterByName(d.character) != null)
        {
            Character speakingCharacter = CharacterManager.Instance.GetCharacterByName(d.character);

            // Update previous and current character
            // previousCharacterType = currentCharacterType;
            currentCharacterType = speakingCharacter.type;
            // CameraManager.Instance.FocusOn(speakingCharacter.transform);
            // OnEpisodeStart?.Invoke();

            // Turn the current speaker towards the previous speaker
            // if (previousCharacterType != CharacterType.None)
            // {
            //     Character previousCharacter = CharacterManager.Instance.GetCharacterByType(previousCharacterType);
            //     StartCoroutine(TurnToSpeaker(speakingCharacter.transform, previousCharacter.transform));
            // }

            // yield return new WaitForSeconds(1);

            // foreach (Character character in _characters)
            // {
            //     if (currentCharacterType == character.type) continue;
            //     StartCoroutine(TurnToSpeaker(character.transform, speakingCharacter.transform));
            // }
        }

        if (subtitles != null)
            subtitles.text = d.text;

        using (
            var uwr = UnityWebRequestMultimedia
                .GetAudioClip(d.audioPath, AudioType.UNKNOWN)
            )
        {
            yield return uwr.SendWebRequest();
            AudioClip downloadedClip = DownloadHandlerAudioClip
                                        .GetContent(uwr);
            Character character = CharacterManager.Instance
                .GetCharacterByName(d.character);
            // TODO: for some reason sphereDoomer object is not found
            // GameObject sphereDoomer = characterPrefabs[0];
            // AudioSource audioSource = sphereDoomer.GetComponent<AudioSource>();
            // string[] texts = {};
            // List<Dialogue2> dialogue2s = new List<Dialogue2>();
            // Task.Run()
            // Task 1d = CreateTTSRequestTasksAsync(texts, dialogue2s);

            // if (character.TryGetComponent(out AudioVoice voice))
            // {

            // }
            if (!audioSource)
            {
                audioSource = GetComponent<AudioSource>();
                if (!audioSource) 
                {
                    audioSource = gameObject
                                    .AddComponent<AudioSource>();
                }
            }
            audioSource.clip = downloadedClip;
            audioSource.Play();
            float startTime = Time.time;
            while (audioSource.isPlaying && 
                    Time.time - startTime <= 60.0f)
            {
                yield return null;
            }
        }

        // using (var uwr = UnityWebRequestMultimedia.GetAudioClip($"https://storage.googleapis.com/vocodes-public{v.state.maybe_public_bucket_wav_audio_path}", AudioType.WAV))
        // {
        //     yield return uwr.SendWebRequest();
        //     if (uwr.result == UnityWebRequest.Result.ConnectionError)
        //     {
        //         Debug.Log(uwr.error);
        //     }
        //     else
        //     {
        //         //NEW IMPLEMENTATION

        //         //Grab audio and references
        //         AudioClip downloadedClip = DownloadHandlerAudioClip.GetContent(uwr);
        //         //d.character.
        //         Character character = CharacterManager.Instance.GetCharacterByName(d.character);
        //         CharacterManager.Instance.ResetSpeakingFlagForCharacters();

        //         //Make sure audioSource is assigned
        //         if (!audioSource)
        //         {
        //             audioSource = GetComponent<AudioSource>();
        //             if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
        //         }


        //         if (character != null)
        //         {
        //             //Use audioclip for gary or actual audio for anyone else
        //             AudioClip selectedClip = character.type == CharacterType.Gary ? audioClips[0] : downloadedClip;
        //             if (character.TryGetComponent(out AudioVoice voice))
        //             {
        //                 //Use new audio system
        //                 voice.Init(selectedClip);
        //                 voice.Play();
        //                 audioSource = voice.Source;
        //             }
        //             else
        //             {

        //                 audioSource.clip = selectedClip;
        //                 audioSource.bypassEffects = true;
        //                 audioSource.bypassListenerEffects = true;
        //                 audioSource.bypassReverbZones = true;
        //                 audioSource.Play();
        //             }
        //             OnCharacterSpeaking?.Invoke(character, d.text);
        //             character.StartSpeaking();
        //         }
        //         else
        //         {
        //             IntroController.Instance.IntroNarrator(d.text);

        //             //Use old implementation if something goes wrong..
        //             audioSource.clip = downloadedClip;
        //             audioSource.bypassEffects = true;
        //             audioSource.bypassListenerEffects = true;
        //             audioSource.bypassReverbZones = true;
        //             audioSource.Play();
        //             Debug.LogWarning("[AIThing] Using old audio system for this character! ");
        //         }


        //         //Wait for audio to finish playing...
        //         float startTime = Time.time;
        //         while (audioSource.isPlaying && Time.time - startTime <= 60.0f)
        //         {
        //             yield return null;
        //         }

        //         if (audioSource.isPlaying)
        //         {
        //             //Stop audio and animation
        //             audioSource.Stop();
        //             if (character != null) character.StopSpeaking();
        //         }
        //     }
        // }
    }

    private IEnumerator Speak(Dialogue d)
    {
        if (retryCount >= maxRetries) // Check if maximum retries have been reached
        {
            Debug.LogWarning("Max retries reached, skipping dialogue");
            OnDialogueLineFullyGenerated?.Invoke(-2);
            retryCount = 0; // Reset the retry counter
            // Add logic here to skip dialogue or handle the timeout as needed
            yield break; // Exit the coroutine
        }
        var content = _client.GetAsync($"https://api.fakeyou.com/tts/job/{d.uuid}").Result.Content;
        var responseContent = content.ReadAsStringAsync().Result;
        var v = JsonConvert.DeserializeObject<GetResponse>(responseContent);
        Debug.Log(responseContent);

        if (v.state == null || v.state.status == "pending" || v.state.status == "started" || v.state.status == "attempt_failed")
        {
            yield return new WaitForSeconds(1.5f);
            retryCount++; // Increment retry counter
            yield return Speak(d);
        }
        else if (v.state.status == "complete_success")
        {
            retryCount = 0; // Reset the retry counter
            yield return HandleSuccessfulTTSRequest(d, v);
        }
        else
        {
            string newUuid = null;
            yield return CreateNewVoiceRequest(d, result => { newUuid = result; });

            if (!string.IsNullOrEmpty(newUuid))
            {
                d.uuid = newUuid;
                yield return Speak(d);
            }
            else
            {
                Debug.LogError("Failed to create new voice request");
                OnDialogueLineFullyGenerated?.Invoke(-2);
            }
        }
    }

    private IEnumerator HandleSuccessfulTTSRequest(
        Dialogue d, GetResponse v
    )
    {
        if (CharacterManager.Instance
                .GetCharacterByName(d.character) != null)
        {
            Character speakingCharacter = CharacterManager.Instance
                .GetCharacterByName(d.character);

            // Update previous and current character
            previousCharacterType = currentCharacterType;
            currentCharacterType = speakingCharacter.type;
            CameraManager.Instance.FocusOn(speakingCharacter.transform);
            OnEpisodeStart?.Invoke();

            // Turn the current speaker towards the previous speaker
            if (previousCharacterType != CharacterType.None)
            {
                Character previousCharacter = CharacterManager.Instance.GetCharacterByType(previousCharacterType);
                StartCoroutine(TurnToSpeaker(speakingCharacter.transform, previousCharacter.transform));
            }

            yield return new WaitForSeconds(1);

            foreach (Character character in _characters)
            {
                if (currentCharacterType == character.type) continue;
                StartCoroutine(TurnToSpeaker(character.transform, speakingCharacter.transform));
            }
        }

        if (subtitles != null)
            subtitles.text = d.text;

        using (var uwr = UnityWebRequestMultimedia.GetAudioClip($"https://storage.googleapis.com/vocodes-public{v.state.maybe_public_bucket_wav_audio_path}", AudioType.WAV))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                //NEW IMPLEMENTATION

                //Grab audio and references
                AudioClip downloadedClip = DownloadHandlerAudioClip.GetContent(uwr);
                //d.character.
                Character character = CharacterManager.Instance.GetCharacterByName(d.character);
                CharacterManager.Instance.ResetSpeakingFlagForCharacters();

                //Make sure audioSource is assigned
                if (!audioSource)
                {
                    audioSource = GetComponent<AudioSource>();
                    if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
                }


                if (character != null)
                {
                    //Use audioclip for gary or actual audio for anyone else
                    AudioClip selectedClip = character.type == CharacterType.Gary ? audioClips[0] : downloadedClip;
                    if (character.TryGetComponent(out AudioVoice voice))
                    {
                        //Use new audio system
                        voice.Init(selectedClip);
                        voice.Play();
                        audioSource = voice.Source;
                    }
                    else
                    {

                        audioSource.clip = selectedClip;
                        audioSource.bypassEffects = true;
                        audioSource.bypassListenerEffects = true;
                        audioSource.bypassReverbZones = true;
                        audioSource.Play();
                    }
                    OnCharacterSpeaking?.Invoke(character, d.text);
                    character.StartSpeaking();
                }
                else
                {
                    IntroController.Instance.IntroNarrator(d.text);

                    //Use old implementation if something goes wrong..
                    audioSource.clip = downloadedClip;
                    audioSource.bypassEffects = true;
                    audioSource.bypassListenerEffects = true;
                    audioSource.bypassReverbZones = true;
                    audioSource.Play();
                    Debug.LogWarning("[AIThing] Using old audio system for this character! ");
                }


                //Wait for audio to finish playing...
                float startTime = Time.time;
                while (audioSource.isPlaying && Time.time - startTime <= 60.0f)
                {
                    yield return null;
                }

                if (audioSource.isPlaying)
                {
                    //Stop audio and animation
                    audioSource.Stop();
                    if (character != null) character.StopSpeaking();
                }
            }
        }
    }
}