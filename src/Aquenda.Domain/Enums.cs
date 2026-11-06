namespace Aquenda.Domain;

/// <summary>Grandes áreas do catálogo, uma por aba do app.</summary>
public enum CategoriaServico
{
    Acolhimento = 1,
    CentroReferencia = 2,
    Saude = 3,
    Juridico = 4,
    Role = 5
}

/// <summary>
/// O que o serviço oferece. É um enum de flags: um Centro de Referência pode
/// ter atendimento psicossocial e jurídico ao mesmo tempo.
/// </summary>
[Flags]
public enum Atendimento
{
    Nenhum = 0,
    Psicossocial = 1 << 0,
    Juridico = 1 << 1,
    Hormonizacao = 1 << 2,
    SaudeMental = 1 << 3,
    Retificacao = 1 << 4,
    Denuncia = 1 << 5,
    Acolhida = 1 << 6,
    Trabalho = 1 << 7,
    Convivencia = 1 << 8
}

public enum TipoAtencao
{
    Primaria = 1,
    Secundaria = 2
}

public enum TipoRole
{
    Bar = 1,
    Balada = 2,
    Cultural = 3,
    EspacoPublico = 4
}

/// <summary>De onde veio a leitura de ocupação de uma casa.</summary>
public enum OrigemLeitura
{
    Sensor = 1,
    Manual = 2,
    Simulado = 3
}

/// <summary>Eventos anônimos usados no indicador de impacto.</summary>
public enum TipoInteracao
{
    Busca = 1,
    ComoChegar = 2,
    Ligar = 3,
    AbrirSite = 4
}
