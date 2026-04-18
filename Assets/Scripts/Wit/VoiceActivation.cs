using AYellowpaper.SerializedCollections;
using Meta.WitAi;
using NUnit.Framework;
using Oculus.Voice;
using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Añade funcionalidad genérica a los eventos recibidos por Wit,
/// además de activar permanentemente el micrófono cuando el jugador 
/// esté hablando 
/// </summary>
public class VoiceActivation : Singleton<VoiceActivation>
{
    [Header("References")]
    [SerializeField] 
    AppVoiceExperience appVoiceExperience;

    [Header("Analytics")]
    // Diccionario que guarda el nombre de la intención y cuántas veces ha sido llamada.
    public System.Collections.Generic.Dictionary<string, int> IntentUsageCount = new System.Collections.Generic.Dictionary<string, int>();

    [Header("Debug")]
    [SerializeField] 
    WitDebugPanel _debugPanel;
    TMPro.TextMeshProUGUI _debugText;

    [Header("Events")]
    /// <summary>
    /// Diccionario que contiene una intención y el método al que queremos
    /// llamar cuando se registre una entrada con dicha intención
    /// </summary>
    [SerializeField, SerializedDictionary("Intention", "On response to intention")]
    SerializedDictionary<Intention, UnityEvent<WitMessageData>> _onResponseToIntent;

    #region events
    private UnityEvent<WitMessageData> _onValidatePartialResponse;
    public UnityEvent<WitMessageData> OnValidatePartialResponse { get { return _onValidatePartialResponse; } }
    #endregion 

    void Start()
    {
        // st = ClassManager.Instance.GetStudentsController();
        _debugText = _debugPanel?.GetComponentInChildren<TMPro.TextMeshProUGUI>();
    }

    /// <summary>
    /// Activa o desactiva el panel de debug de información
    /// </summary>
    /// <param name="active"> Si se activa o no </param>
    public void ActivateDebugPanel(bool active)
    {
        if (!_debugPanel) return;
        _debugPanel.enabled = active;
    }

    /// <summary>
    /// Activa el reconocimiento de voz
    /// </summary>
    public void ActivateVoice()
    {
        if (appVoiceExperience != null)
            appVoiceExperience.Activate();
    }

    protected override void Awake()
    {
        base.Awake();
        // El micrófono no se activará automáticamente al iniciar (sirve para evitar la detección de ruido).
        AddVoiceListeners();
    }

    void Update()
    {
        // Pulsa la barra espaciadora para que el micrófono funcione.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Micrófono abierto");
            ActivateVoice();
        }
    }

    void AddVoiceListeners()
    {
        if (_onValidatePartialResponse == null)
        {
            _onValidatePartialResponse = new UnityEvent<WitMessageData>();
        }

        _onValidatePartialResponse.AddListener(ChangeSelectedStudents);

        appVoiceExperience.VoiceEvents.OnError.AddListener((error, message) =>
        {
            Debug.LogError($"Erro do Wit: {error} - {message}");
        });

        appVoiceExperience.VoiceEvents.OnResponse.AddListener((response) =>
        {
            OnResponse(response);
        });

        appVoiceExperience.VoiceEvents.OnValidatePartialResponse.AddListener((sessionData) =>
        {
            _onValidatePartialResponse.Invoke(MakeMessage(sessionData.response));
        });

        appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener((transcription) =>
        {
            SetMainPanelText(transcription);
        });
    }

    #region DEBUG PANEL
    void SetMainPanelText(string text)
    {
        _debugPanel.SetMainText(text);
    }

    /// <summary>
    /// Monta un string con los estudiantes resaltados en color azul y separados
    /// entre comas con un punto al final.
    /// </summary>
    /// <param name="data"> Nombres de alumnos seleccionados </param>
    /// <returns> Estudiantes seleccionados </returns>
    string GetStudentsInData(System.Collections.Generic.List<string> data)
    {
        string message = "";
        int i = 0;
        foreach (string name in data)
        {
            message += "<color=blue>" + name + "</color>";
            if (i != data.Count - 1)
            {
                message += ", ";
            }
            else
            {
                message += '.';
            }
            ++i;
        }
        return message;
    }

    /// <summary>
    /// Añade un panel de DEBUG con los estudiantes seleccionados
    /// </summary>
    /// <param name="data"> Datos de respuesta de Wit </param>
    void ChangeSelectedStudents(WitMessageData data)
    {
        string students = GetStudentsInData(StudentManager.Instance.GetSelectedStudents());
        if (students == "") return;

        string message = "Selected: " + students;
        _debugPanel.ChangeStudentPanel(message);
    }

    /// <summary>
    /// Añade un panel con la intención a ejecutar y los estudiantes afectados
    /// </summary>
    /// <param name="data"> Datos de respuesta de Wit </param>
    void AddPanelWithIntent(WitMessageData data)
    {
        if (data.Intention == Intention.None) return;

        string message = data.Intention.ToString()
            + ": " + GetStudentsInData(StudentManager.Instance.GetSelectedStudents());

        _debugPanel.AddPanel(message);
    }
    #endregion

    /// <summary>
    /// Generamos el mensaje según los datos de la sesión de Wit
    /// Estos datos rellenan el nombre de los estudiantes afectados y la intención del mensaje
    /// </summary>
    /// <param name="response"> Datos de la sesión de Wit </param>
    /// <returns> Información del mensaje transcrito </returns>
    WitMessageData MakeMessage(Meta.WitAi.Json.WitResponseNode response)
    {
        // 1. Detiene la ejecución en caso de que no haya respuesta del servidor.
        if (response == null) return new WitMessageData { Intention = Intention.None };

        WitMessageData messageData = new WitMessageData();
        messageData.Names = new System.Collections.Generic.List<string>();

        // 2. Busca el nombre de los alumnos.
        string[] names = null;
        try { names = response.GetAllEntityValues("wit$contact:contact"); } catch { }

        if (names != null && StudentManager.Instance != null)
        {
            foreach (string name in names)
            {
                if (!string.IsNullOrEmpty(name) && StudentManager.Instance.GetStudent(name) != null)
                {
                    messageData.Names.Add(name);
                }
            }
        }

        string intentString = response.GetIntentName();
        Debug.Log($"O Wit compreendeu: '{response.GetTranscription()}'. La intención devuelta por el servidor ha sido: '{intentString}'");

        Intention intent = Intention.None;
        if (!string.IsNullOrEmpty(intentString))
        {
            Enum.TryParse(intentString, true, out intent);
        }

        messageData.Intention = intent;
        messageData.Transcription = response.GetTranscription();

        return messageData;
    }

    /// <summary>
    /// Recibimos la respuesta final de Wit.
    /// La parseamos e invocamos a los eventos correspondientes para que
    /// se activen según hemos designado en el inspector
    /// </summary>
    /// <param name="response"> Respuesta final de Wit </param>
    public void OnResponse(Meta.WitAi.Json.WitResponseNode response)
    {
        WitMessageData messageData = MakeMessage(response);
        string intentName = messageData.Intention.ToString();

        if (messageData.Intention == Intention.None)
        {
            Debug.Log($"Wit ha oído: '{messageData.Transcription}', pero no ha encontrado ninguna intención.");
            return;
        }

        if (IntentUsageCount.ContainsKey(intentName))
        {
            IntentUsageCount[intentName]++; 
        }
        else
        {
            IntentUsageCount.Add(intentName, 1); 
        }

        FirebaseManager.RegistrarUsoDeIntent(intentName, IntentUsageCount[intentName]);
        Debug.Log($"Intent {intentName} se guarda en Firebase con el valor: {IntentUsageCount[intentName]}.");

        if (_onResponseToIntent.ContainsKey(messageData.Intention))
        {
            _onResponseToIntent[messageData.Intention].Invoke(messageData);
        }
        else
        {
            Debug.LogWarning($"La intent '{intentName}' ha sido reconocida, ¡pero no está configurada en la lista del Inspector!");
        }

        AddPanelWithIntent(messageData);
    }
}
