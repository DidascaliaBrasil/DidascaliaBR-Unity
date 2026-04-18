using System.Collections.Generic;
using System;

/// <summary>
/// Representa la intención de un mensaje de Wit
/// </summary>
[Serializable]
public enum Intention
{
    None = 0,
    Acalmar = 1,
    Acolher = 2,
    Adiar = 3,
    Advertencia = 4,
    AjustarRitmo = 5,
    BajarVoz = 6,
    Castigo = 7,
    ChamarAluno = 8,
    Compreensao = 9,
    Cumprimentos = 10,
    DarApoio = 11,
    Despedida = 12,
    ElogioPositivo = 13,
    EstabelecerLimites = 14,
    ExplicarNovamente = 15,
    Expulsao = 16,
    Impaciencia = 17,
    IncentivarAutonomia = 18,
    MoverAluno = 19,
    NegociarAcordo = 20,
    Parabenizar = 21,
    PararConflito = 22,
    PausarAula = 23,
    PedirApoio = 24,
    Perguntar = 25,
    PromoverRespeito = 26,
    ProporAlternativa = 27,
    ReforcarRegra = 28,
    RegularEmocao = 29,
    SacarMaterial = 30,
    Sentarse = 31,
    Silencio = 32,
    Trabalhar = 33,
    TrocarAluno = 34,
    TrocarAtividade = 35
}

/// <summary>
/// Estructura que guarda la intención de una transcripción de wit, además
/// de sus correspondientes estudiantes afectados
/// </summary>
public struct WitMessageData
{
    public List<string> Names;
    public Intention Intention;
    public string Transcription;
}