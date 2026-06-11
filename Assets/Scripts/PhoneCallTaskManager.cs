using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649

[Serializable]
public class CallData
{
    [Header("Ses Kaydi")]
    public AudioClip konusmaKaydi;

    [Header("Dogru Cevaplar")]
    public string isimSoyisim;
    public string departman;
    public string telefonNumarasi;

    [TextArea(2, 5)]
    public string not;
}

public sealed class PhoneCallTaskManager : MonoBehaviour
{
    private const string ControllerName = "_PhoneCallTaskManager";
    private const OfficeTaskKind TaskKind = OfficeTaskKind.Phone;

    [Header("Arama Verileri")]
    [SerializeField] private List<CallData> aramalar = new List<CallData>();
    [SerializeField] private bool rastgeleAramaSec;

    [Header("Zamanlama")]
    [SerializeField, Min(0f)] private float ilkAramaGecikmesi = 15f;
    [SerializeField, Min(1f)] private float aramaAraligiMin = 20f;
    [SerializeField, Min(1f)] private float aramaAraligiMax = 35f;
    [SerializeField, Min(1f)] private float cevaplanmazsaKapanmaSuresi = 20f;
    [SerializeField] private bool oyunBaslayincaOtomatikBaslat = true;
    [SerializeField] private bool konusmaBitinceOtomatikKontrolEt = true;

    [Header("Ses")]
    [SerializeField] private AudioSource zilAudioSource;
    [SerializeField] private AudioSource konusmaAudioSource;
    [SerializeField] private AudioClip zilSesi;
    [SerializeField] private List<AudioClip> zilSesleri = new List<AudioClip>();
    [SerializeField] private bool resourcesPhoneKlasorundenOtomatikYukle = true;
    [SerializeField] private string phoneAudioResourcesPath = "Audio/Phone";

    [Header("UI")]
    [SerializeField] private GameObject telefonPaneli;
    [SerializeField] private GameObject notDefteriPaneli;
    [SerializeField] private Button acButonu;
    [SerializeField] private Button tekrarDinleButonu;
    [SerializeField] private Button kontrolEtButonu;
    [SerializeField] private TMP_InputField isimSoyisimInput;
    [SerializeField] private TMP_InputField departmanInput;
    [SerializeField] private TMP_InputField telefonNumarasiInput;
    [SerializeField] private TMP_Text sonucText;

    [Header("Mesajlar")]
    [SerializeField] private string gelenAramaMesaji = "Telefon caliyor...";
    [SerializeField] private string konusmaBasladiMesaji = "Gorusme basladi. Bilgileri not al.";
    [SerializeField] private string tekrarDinleMesaji = "Gorusme tekrar oynatiliyor.";
    [SerializeField] private string basariliMesaj = "Gorev basarili.";
    [SerializeField] private string cevapBekleniyorMesaji = "Once telefonu acmalisin.";

    private Coroutine callLoopCoroutine;
    private Coroutine unansweredTimeoutCoroutine;
    private Coroutine activeConversationCoroutine;
    private CallData aktifArama;
    private AudioClip[] phoneAudioClips = Array.Empty<AudioClip>();
    private AudioClip generatedRingClip;
    private TaskTimer taskTimer;
    private int siradakiAramaIndex;
    private int siradakiZilIndex;
    private int dogruAramaSayisi;
    private int yanlisAramaSayisi;
    private bool telefonCaliyor;
    private bool aramaCevaplandi;
    private bool aramaAktif;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void RegisterSceneLoadedHook()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
        ClickableDeskObject.Clicked -= HandleDeskObjectClicked;
        ClickableDeskObject.Clicked += HandleDeskObjectClicked;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForInitialScene()
    {
        EnsureManagerForScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        EnsureManagerForScene(scene);
    }

    private static void EnsureManagerForScene(UnityEngine.SceneManagement.Scene scene)
    {
        if (!IsLevelScene(scene.name) || FindAnyObjectByType<PhoneCallTaskManager>() != null)
        {
            return;
        }

        GameObject managerObject = new GameObject(ControllerName);
        managerObject.AddComponent<PhoneCallTaskManager>();
    }

    private static bool IsLevelScene(string sceneName)
    {
        LevelDefinition level = LevelDatabase.Load().GetLevelBySceneName(sceneName);
        return level != null;
    }

    private void Reset()
    {
        aramalar = CreateDefaultCalls();
        ilkAramaGecikmesi = 15f;
        resourcesPhoneKlasorundenOtomatikYukle = true;
    }

    private void Awake()
    {
        EnsureAudioSources();
        LoadPhoneAudioFromResources();
        EnsureDefaultCalls();
        AutoAssignAudioClips();
        BindButtons();
        SetUiVisible(false);
        SetInputsInteractable(false);
        SetReplayInteractable(false);
    }

    private void OnEnable()
    {
        ClickableDeskObject.Clicked -= HandleDeskObjectClicked;
        ClickableDeskObject.Clicked += HandleDeskObjectClicked;
        TaskAssignmentSession.AssignmentsChanged -= HandleTaskAssignmentsChanged;
        TaskAssignmentSession.AssignmentsChanged += HandleTaskAssignmentsChanged;

        if (oyunBaslayincaOtomatikBaslat && TaskAssignmentSession.HasAssignments)
        {
            StartConfiguredPhoneTask();
        }
    }

    private void OnDisable()
    {
        TaskAssignmentSession.AssignmentsChanged -= HandleTaskAssignmentsChanged;
        StopCallLoop();
        StopCurrentCall();
        UnbindButtons();
    }

    private void HandleTaskAssignmentsChanged()
    {
        if (oyunBaslayincaOtomatikBaslat)
        {
            StartConfiguredPhoneTask(false);
        }
    }

    private static void HandleDeskObjectClicked(ClickableDeskObject clickedObject)
    {
        if (!OfficeMiniGameUi.MatchesClickedObject(clickedObject, null, "telefon", "phone"))
        {
            return;
        }

        PhoneCallTaskManager manager = FindAnyObjectByType<PhoneCallTaskManager>();
        if (manager != null)
        {
            manager.StartConfiguredPhoneTask(true);
        }
    }

    private void StartConfiguredPhoneTask(bool startImmediately = false)
    {
        if (!TaskAssignmentSession.IsTaskEnabled(TaskKind))
        {
            StopCallLoop();
            StopCurrentCall();
            return;
        }

        dogruAramaSayisi = 0;
        yanlisAramaSayisi = 0;
        StartTaskTimer();
        if (startImmediately)
        {
            StartNextCallNow();
        }
        else
        {
            StartCallLoop();
        }
    }

    public void StartCallLoop()
    {
        if (callLoopCoroutine != null)
        {
            return;
        }

        callLoopCoroutine = StartCoroutine(CallLoop());
    }

    public void StopCallLoop()
    {
        if (callLoopCoroutine != null)
        {
            StopCoroutine(callLoopCoroutine);
            callLoopCoroutine = null;
        }
    }

    public void StartNextCallNow()
    {
        if (aramaAktif)
        {
            return;
        }

        StartIncomingCall(SelectNextCall());
    }

    public void AnswerCall()
    {
        if (!telefonCaliyor || aktifArama == null)
        {
            SetResult(cevapBekleniyorMesaji);
            return;
        }

        aramaCevaplandi = true;
        telefonCaliyor = false;

        if (unansweredTimeoutCoroutine != null)
        {
            StopCoroutine(unansweredTimeoutCoroutine);
            unansweredTimeoutCoroutine = null;
        }

        StopRing();
        SetInputsInteractable(true);
        SetResult(konusmaBasladiMesaji);

        if (aktifArama.konusmaKaydi == null)
        {
            SetResult("Bu arama icin konusma kaydi atanmamis.");
            SetReplayInteractable(false);
            return;
        }

        PlayConversationAudio(konusmaBasladiMesaji);
    }

    public void ReplayCallAudio()
    {
        if (!aramaCevaplandi || aktifArama == null)
        {
            SetResult(cevapBekleniyorMesaji);
            return;
        }

        if (aktifArama.konusmaKaydi == null)
        {
            SetResult("Bu arama icin konusma kaydi atanmamis.");
            SetReplayInteractable(false);
            return;
        }

        PlayConversationAudio(tekrarDinleMesaji);
    }

    private void PlayConversationAudio(string statusMessage)
    {
        if (activeConversationCoroutine != null)
        {
            StopCoroutine(activeConversationCoroutine);
            activeConversationCoroutine = null;
        }

        konusmaAudioSource.Stop();
        konusmaAudioSource.clip = aktifArama.konusmaKaydi;
        konusmaAudioSource.loop = false;
        konusmaAudioSource.Play();

        SetReplayInteractable(true);
        SetResult(statusMessage);
        activeConversationCoroutine = StartCoroutine(WaitForConversationEnd());
    }

    public void CheckAnswers()
    {
        if (aktifArama == null)
        {
            SetResult("Aktif arama yok.");
            return;
        }

        List<string> wrongFields = new List<string>();

        if (!TextMatches(isimSoyisimInput, aktifArama.isimSoyisim))
        {
            wrongFields.Add("Isim Soyisim");
        }

        if (!TextMatches(departmanInput, aktifArama.departman))
        {
            wrongFields.Add("Departman");
        }

        if (!PhoneMatches(telefonNumarasiInput, aktifArama.telefonNumarasi))
        {
            wrongFields.Add("Telefon Numarasi");
        }

        if (wrongFields.Count == 0)
        {
            dogruAramaSayisi++;
            CompleteCurrentCall(true, basariliMesaj);
            return;
        }

        yanlisAramaSayisi++;
        SetResult("Yanlis alanlar: " + string.Join(", ", wrongFields));
    }

    private void StartTaskTimer()
    {
        if (!TaskAssignmentSession.TryGetAssignment(TaskKind, out TaskAssignment assignment))
        {
            return;
        }

        if (taskTimer == null)
        {
            taskTimer = gameObject.AddComponent<TaskTimer>();
        }

        taskTimer.TimerExpired -= HandleTaskTimerExpired;
        taskTimer.TimerExpired += HandleTaskTimerExpired;
        taskTimer.StartTimer(assignment.TimeLimitSeconds);
        TaskAssignmentSession.RegisterTaskTimer(TaskKind, taskTimer);
    }

    private void HandleTaskTimerExpired(TaskTimer expiredTimer)
    {
        StopCallLoop();
        StopCurrentCall();
        TaskAssignmentSession.MarkTaskFailed(TaskKind);
        int accuracy = TaskAssignmentSession.CalculateAccuracyPercent(dogruAramaSayisi, yanlisAramaSayisi);
        SetUiVisible(true);
        SetInputsInteractable(false);
        SetReplayInteractable(false);
        SetResult($"Süre bitti.\nDoğru arama: {dogruAramaSayisi}\nYanlış deneme: {yanlisAramaSayisi}\n{TaskAssignmentSession.BuildAccuracyLine(TaskKind, accuracy)}");
    }

    [ContextMenu("Varsayilan arama cevaplarini yukle")]
    private void LoadDefaultCalls()
    {
        aramalar = CreateDefaultCalls();
    }

    private IEnumerator CallLoop()
    {
        yield return new WaitForSeconds(ilkAramaGecikmesi);

        while (enabled)
        {
            if (!aramaAktif)
            {
                StartIncomingCall(SelectNextCall());
            }

            while (aramaAktif)
            {
                yield return null;
            }

            float waitTime = UnityEngine.Random.Range(
                Mathf.Min(aramaAraligiMin, aramaAraligiMax),
                Mathf.Max(aramaAraligiMin, aramaAraligiMax));

            yield return new WaitForSeconds(waitTime);
        }
    }

    private void StartIncomingCall(CallData callData)
    {
        if (callData == null)
        {
            SetResult("Arama listesi bos.");
            return;
        }

        aktifArama = callData;
        aramaAktif = true;
        telefonCaliyor = true;
        aramaCevaplandi = false;

        ClearInputs();
        SetUiVisible(true);
        SetInputsInteractable(false);
        SetReplayInteractable(false);
        SetResult(gelenAramaMesaji);
        StartRing();

        if (unansweredTimeoutCoroutine != null)
        {
            StopCoroutine(unansweredTimeoutCoroutine);
        }

        unansweredTimeoutCoroutine = StartCoroutine(CloseIfUnanswered());
    }

    private CallData SelectNextCall()
    {
        if (aramalar == null || aramalar.Count == 0)
        {
            return null;
        }

        if (rastgeleAramaSec)
        {
            return aramalar[UnityEngine.Random.Range(0, aramalar.Count)];
        }

        CallData selected = aramalar[siradakiAramaIndex % aramalar.Count];
        siradakiAramaIndex++;
        return selected;
    }

    private IEnumerator CloseIfUnanswered()
    {
        yield return new WaitForSeconds(cevaplanmazsaKapanmaSuresi);

        if (!aramaCevaplandi && telefonCaliyor)
        {
            yanlisAramaSayisi++;
            CompleteCurrentCall(false, "Arama cevaplanmadi.");
        }
    }

    private IEnumerator WaitForConversationEnd()
    {
        while (konusmaAudioSource != null && konusmaAudioSource.isPlaying)
        {
            yield return null;
        }

        if (konusmaBitinceOtomatikKontrolEt)
        {
            CheckAnswers();
        }
        else
        {
            SetResult("Gorusme bitti. Bilgileri kontrol edebilirsin.");
        }
    }

    private void CompleteCurrentCall(bool success, string message)
    {
        StopCurrentCall();
        SetResult(message);

        if (success)
        {
            ClearInputs();
            SetUiVisible(false);
        }
    }

    private void StopCurrentCall()
    {
        telefonCaliyor = false;
        aramaCevaplandi = false;
        aramaAktif = false;
        aktifArama = null;

        if (unansweredTimeoutCoroutine != null)
        {
            StopCoroutine(unansweredTimeoutCoroutine);
            unansweredTimeoutCoroutine = null;
        }

        if (activeConversationCoroutine != null)
        {
            StopCoroutine(activeConversationCoroutine);
            activeConversationCoroutine = null;
        }

        StopRing();

        if (konusmaAudioSource != null)
        {
            konusmaAudioSource.Stop();
        }
    }

    private void StartRing()
    {
        if (zilAudioSource == null)
        {
            return;
        }

        AudioClip selectedRing = SelectRingClip();
        if (selectedRing == null)
        {
            selectedRing = GetGeneratedRingClip();
        }

        zilAudioSource.Stop();
        zilAudioSource.clip = selectedRing;
        zilAudioSource.loop = true;
        zilAudioSource.Play();
    }

    private AudioClip SelectRingClip()
    {
        if (zilSesleri != null)
        {
            zilSesleri.RemoveAll(clip => clip == null);
        }

        if (zilSesleri != null && zilSesleri.Count > 0)
        {
            AudioClip selected = zilSesleri[siradakiZilIndex % zilSesleri.Count];
            siradakiZilIndex++;
            return selected;
        }

        return zilSesi;
    }

    private AudioClip GetGeneratedRingClip()
    {
        if (generatedRingClip != null)
        {
            return generatedRingClip;
        }

        const int sampleRate = 44100;
        const float duration = 0.8f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            bool activePulse = t < 0.32f || (t > 0.42f && t < 0.68f);
            samples[i] = activePulse ? Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.22f : 0f;
        }

        generatedRingClip = AudioClip.Create("Generated Phone Ring", sampleCount, 1, sampleRate, false);
        generatedRingClip.SetData(samples, 0);
        return generatedRingClip;
    }

    private void StopRing()
    {
        if (zilAudioSource != null)
        {
            zilAudioSource.Stop();
        }
    }

    private void EnsureAudioSources()
    {
        if (zilAudioSource == null)
        {
            zilAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (konusmaAudioSource == null)
        {
            konusmaAudioSource = gameObject.AddComponent<AudioSource>();
        }

        zilAudioSource.playOnAwake = false;
        konusmaAudioSource.playOnAwake = false;
    }

    private void LoadPhoneAudioFromResources()
    {
        if (!resourcesPhoneKlasorundenOtomatikYukle)
        {
            return;
        }

        phoneAudioClips = Resources.LoadAll<AudioClip>(phoneAudioResourcesPath);
        if (phoneAudioClips == null)
        {
            phoneAudioClips = Array.Empty<AudioClip>();
        }

        if ((zilSesi == null && (zilSesleri == null || zilSesleri.Count == 0)) && phoneAudioClips.Length > 0)
        {
            AudioClip ringClip = FindPhoneClip("telefon sesi", "telefon", "zil", "ring");
            if (ringClip != null)
            {
                zilSesi = ringClip;
                if (zilSesleri == null)
                {
                    zilSesleri = new List<AudioClip>();
                }

                zilSesleri.Add(ringClip);
            }
        }
    }

    private void EnsureDefaultCalls()
    {
        if (aramalar == null)
        {
            aramalar = new List<CallData>();
        }

        if (aramalar.Count == 0)
        {
            aramalar = CreateDefaultCalls();
        }
    }

    private void AutoAssignAudioClips()
    {
        if (!resourcesPhoneKlasorundenOtomatikYukle || aramalar == null || aramalar.Count == 0)
        {
            return;
        }

        for (int i = 0; i < aramalar.Count; i++)
        {
            CallData call = aramalar[i];
            if (call == null || call.konusmaKaydi != null)
            {
                continue;
            }

            call.konusmaKaydi = FindPhoneClip(BuildNameSearchTerms(call.isimSoyisim));
        }
    }

    private static string[] BuildNameSearchTerms(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Array.Empty<string>();
        }

        string[] parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        List<string> terms = new List<string> { fullName };
        terms.AddRange(parts);
        return terms.ToArray();
    }

    private AudioClip FindPhoneClip(params string[] searchTerms)
    {
        if (phoneAudioClips == null || phoneAudioClips.Length == 0 || searchTerms == null)
        {
            return null;
        }

        for (int i = 0; i < phoneAudioClips.Length; i++)
        {
            AudioClip clip = phoneAudioClips[i];
            if (clip == null)
            {
                continue;
            }

            string normalizedClipName = NormalizeLookup(clip.name);
            for (int termIndex = 0; termIndex < searchTerms.Length; termIndex++)
            {
                string normalizedTerm = NormalizeLookup(searchTerms[termIndex]);
                if (!string.IsNullOrEmpty(normalizedTerm) && normalizedClipName.Contains(normalizedTerm))
                {
                    return clip;
                }
            }
        }

        return null;
    }

    private void BindButtons()
    {
        if (acButonu != null)
        {
            acButonu.onClick.RemoveListener(AnswerCall);
            acButonu.onClick.AddListener(AnswerCall);
        }

        if (kontrolEtButonu != null)
        {
            kontrolEtButonu.onClick.RemoveListener(CheckAnswers);
            kontrolEtButonu.onClick.AddListener(CheckAnswers);
        }

        if (tekrarDinleButonu != null)
        {
            tekrarDinleButonu.onClick.RemoveListener(ReplayCallAudio);
            tekrarDinleButonu.onClick.AddListener(ReplayCallAudio);
        }
    }

    private void UnbindButtons()
    {
        if (acButonu != null)
        {
            acButonu.onClick.RemoveListener(AnswerCall);
        }

        if (kontrolEtButonu != null)
        {
            kontrolEtButonu.onClick.RemoveListener(CheckAnswers);
        }

        if (tekrarDinleButonu != null)
        {
            tekrarDinleButonu.onClick.RemoveListener(ReplayCallAudio);
        }
    }

    private void SetUiVisible(bool isVisible)
    {
        if (telefonPaneli != null)
        {
            telefonPaneli.SetActive(isVisible);
        }

        if (notDefteriPaneli != null)
        {
            notDefteriPaneli.SetActive(isVisible);
        }
    }

    private void SetInputsInteractable(bool isInteractable)
    {
        if (isimSoyisimInput != null)
        {
            isimSoyisimInput.interactable = isInteractable;
        }

        if (departmanInput != null)
        {
            departmanInput.interactable = isInteractable;
        }

        if (telefonNumarasiInput != null)
        {
            telefonNumarasiInput.interactable = isInteractable;
        }

        if (kontrolEtButonu != null)
        {
            kontrolEtButonu.interactable = isInteractable;
        }
    }

    private void SetReplayInteractable(bool isInteractable)
    {
        if (tekrarDinleButonu != null)
        {
            tekrarDinleButonu.interactable = isInteractable;
        }
    }

    private void ClearInputs()
    {
        SetInputText(isimSoyisimInput, string.Empty);
        SetInputText(departmanInput, string.Empty);
        SetInputText(telefonNumarasiInput, string.Empty);
    }

    private void SetResult(string message)
    {
        if (sonucText != null)
        {
            sonucText.text = message;
        }
    }

    private static void SetInputText(TMP_InputField input, string value)
    {
        if (input != null)
        {
            input.SetTextWithoutNotify(value);
        }
    }

    private static bool TextMatches(TMP_InputField input, string expected)
    {
        string actualText = input != null ? input.text : string.Empty;
        return NormalizeText(actualText) == NormalizeText(expected);
    }

    private static bool PhoneMatches(TMP_InputField input, string expected)
    {
        string actualText = input != null ? input.text : string.Empty;
        return DigitsOnly(actualText) == DigitsOnly(expected);
    }

    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = value.Trim().ToLowerInvariant()
            .Replace('ı', 'i')
            .Replace('İ', 'i');

        normalized = normalized.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder(normalized.Length);

        bool previousWasSpace = false;
        for (int i = 0; i < normalized.Length; i++)
        {
            char current = normalized[i];
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(current);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(current))
            {
                if (!previousWasSpace)
                {
                    builder.Append(' ');
                    previousWasSpace = true;
                }

                continue;
            }

            builder.Append(current);
            previousWasSpace = false;
        }

        return builder.ToString().Trim();
    }

    private static string DigitsOnly(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            if (char.IsDigit(value[i]))
            {
                builder.Append(value[i]);
            }
        }

        return builder.ToString();
    }

    private static string NormalizeLookup(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = NormalizeText(value);
        StringBuilder builder = new StringBuilder(normalized.Length);
        for (int i = 0; i < normalized.Length; i++)
        {
            if (char.IsLetterOrDigit(normalized[i]))
            {
                builder.Append(normalized[i]);
            }
        }

        return builder.ToString();
    }

    private static List<CallData> CreateDefaultCalls()
    {
        return new List<CallData>
        {
            new CallData
            {
                isimSoyisim = "Derya Yilmaz",
                departman = "Insan Kaynaklari",
                telefonNumarasi = "0555 120 34 67",
                not = "Insan Kaynaklari: Is basvurusu gorusme planlama."
            },
            new CallData
            {
                isimSoyisim = "Selin Karaca",
                departman = "Hasta Kabul",
                telefonNumarasi = "0542 300 18 90",
                not = "Hastane: Randevu bilgilendirmesi."
            },
            new CallData
            {
                isimSoyisim = "Burak Demir",
                departman = "Hasta Takip",
                telefonNumarasi = "0533 456 78 21",
                not = "Veteriner: Kontrol randevusu."
            },
            new CallData
            {
                isimSoyisim = "Emre Sahin",
                departman = "Teknik Destek",
                telefonNumarasi = "0551 782 44 10",
                not = "Teknik Servis: Ariza kaydi."
            },
            new CallData
            {
                isimSoyisim = "Ayse Korkmaz",
                departman = "Ogrenci Isleri",
                telefonNumarasi = "0507 219 65 32",
                not = "Ogrenci Isleri: Eksik belge bilgisi."
            },
            new CallData
            {
                isimSoyisim = "Caner Arslan",
                departman = "Finans",
                telefonNumarasi = "0544 610 27 85",
                not = "Finans: Odeme islemi bilgilendirmesi."
            },
        };
    }
}

#pragma warning restore CS0649
