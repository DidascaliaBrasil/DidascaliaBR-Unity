using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.SceneManagement;

public class FirebaseManager : MonoBehaviour
{
    [Header("Interface (UI)")]
    public TMP_InputField codigoInputField;
    public Button entrarButton;
    public TextMeshProUGUI feedbackText;

    [Header("Configuração da Cena")]
    [Tooltip("Nome da sala:")]
    public string nomeDaCenaDaSala = "Nome da sala";

    private DatabaseReference databaseReference;

    public static string CodigoDaSalaAtual = "";

    void Start()
    {
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;

        if (feedbackText != null)
        {
            feedbackText.text = "Digite o código da sala";
        }
    }

    public void TentarEntrarNaSala()
    {
        string codigo = codigoInputField.text.Trim();

        if (string.IsNullOrEmpty(codigo))
        {
            feedbackText.text = "Por favor, digite um código!";
            return;
        }

        feedbackText.text = "Buscando sala...";
        entrarButton.interactable = false;

        databaseReference.Child("classroom_configs").Child(codigo).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            entrarButton.interactable = true;

            if (task.IsFaulted)
            {
                feedbackText.text = "Erro de conexão!";
                Debug.LogError("Erro ao buscar no Firebase: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    feedbackText.text = "Sala encontrada! Iniciando criação...";

                    // NOVO: Salvamos o código digitado para usar no final da aula
                    CodigoDaSalaAtual = codigo;

                    ClassroomData sala = new ClassroomData();
                    sala.numBoys = snapshot.Child("numBoys").Exists ? int.Parse(snapshot.Child("numBoys").Value.ToString()) : 5;
                    sala.numGirls = snapshot.Child("numGirls").Exists ? int.Parse(snapshot.Child("numGirls").Value.ToString()) : 5;
                    sala.numDesks = snapshot.Child("numDesks").Exists ? int.Parse(snapshot.Child("numDesks").Value.ToString()) : 10;
                    sala.rows = snapshot.Child("rows").Exists ? int.Parse(snapshot.Child("rows").Value.ToString()) : 2;
                    sala.cols = snapshot.Child("cols").Exists ? int.Parse(snapshot.Child("cols").Value.ToString()) : 5;
                    sala.radius = snapshot.Child("radius").Exists ? float.Parse(snapshot.Child("radius").Value.ToString()) : 3f;

                    int formatoDoSite = snapshot.Child("shape").Exists ? int.Parse(snapshot.Child("shape").Value.ToString()) : 0;

                    if (formatoDoSite == 0)
                    {
                        sala.shape = 2;
                    }
                    else
                    {
                        sala.shape = formatoDoSite;
                    }

                    sala.maxDesksInSemiCircle = snapshot.Child("maxDesksInSemiCircle").Exists ? int.Parse(snapshot.Child("maxDesksInSemiCircle").Value.ToString()) : 7;

                    ClassroomData.DadosCarregados = sala;

                    SceneManager.LoadScene(nomeDaCenaDaSala);
                }
                else
                {
                    feedbackText.text = "Código Inválido! Essa sala não existe.";
                }
            }
        });
    }

    
    public static void RegistrarUsoDeIntent(string intentName, int quantidadeAtual)
    {
        if (string.IsNullOrEmpty(CodigoDaSalaAtual))
        {
            Debug.LogError("ERRO: O Código da Sala está vazio! O envio foi cancelado.");
            return;
        }

        Debug.Log($"Enviando para o Firebase. Caminho: analytics/{CodigoDaSalaAtual}/intents_usadas/{intentName} = {quantidadeAtual}");

        var dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        dbReference.Child("analytics")
                   .Child(CodigoDaSalaAtual)
                   .Child("intents_usadas")
                   .Child(intentName)
                   .SetValueAsync(quantidadeAtual).ContinueWithOnMainThread(task =>
                   {
                       if (task.IsFaulted)
                       {
                           Debug.LogError("O FIREBASE RECUSOU. Erro: " + task.Exception);
                       }
                       else if (task.IsCompleted)
                       {
                           Debug.Log("O Firebase confirmou o salvamento na nuvem.");
                       }
                   });
    }
}