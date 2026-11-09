using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SingularSystem.Models.Enums
{
    public enum Sex
    {
        [Display(Name = "Feminino")]
        Female = 1,
        [Display(Name = "Masculino")]
        Male,
        [Display(Name = "Outro")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum Serie
    {
        [Display(Name = "Educação Infantil")]
        EarlyChildhoodEducation = 1,
        [Display(Name = "1º Ano")]
        FirstYear,
        [Display(Name = "2º Ano")]
        SecondYear,
        [Display(Name = "3º Ano")]
        ThirdYear,
        [Display(Name = "4º Ano")]
        FourthYear,
        [Display(Name = "5º Ano")]
        FifthYear,
        [Display(Name = "6º Ano")]
        SixthYear,
        [Display(Name = "7º Ano")]
        SeventhYear,
        [Display(Name = "8º Ano")]
        EighthYear,
        [Display(Name = "9º Ano")]
        NinthYear,
        [Display(Name = "1º Ano - Ensino Médio")]
        FirstYearHighSchool,
        [Display(Name = "2º Ano - Ensino Médio")]
        SecondYearHighSchool,
        [Display(Name = "3º Ano - Ensino Médio")]
        ThirdYearHighSchool,
        [Display(Name = "Nenhum(a)")]
        None
    }

    //public enum TypeOfDisability
    //{
    //    [Display(Name = "Deficiência Intelectual")]
    //    IntelectualDisability = 1,
    //    [Display(Name = "Deficiência Física")]
    //    PhysicalDisability,
    //    [Display(Name = "Deficiência Auditiva")]
    //    HearingImpairment,
    //    [Display(Name = "Deficiência Visual")]
    //    VisualImpairment,
    //    [Display(Name = "Deficiência Múltipla")]
    //    MultipleDisability,
    //    [Display(Name = "Transtorno do Espectro Autista")]
    //    AutismSpectrumDisorder,
    //    [Display(Name = "Altas Habilidades/Superdotação")]
    //    HighAbilitiesGiftedness,
    //    [Display(Name = "Nenhum(a)")]
    //    None
    //}

    public enum Status
    {
        [Display(Name = "Pendente")]
        Pending = 1,
        [Display(Name = "Completo")]
        Complete,
        [Display(Name = "Não Iniciado")]
        NotInitiated,
        [Display(Name = "Em Andamento")]
        InProgress,
        [Display(Name = "Nenhum(a)")]
        None = 5,
        [Display(Name = "Recusado")]
        Declined = 6,
        [Display(Name = "Expirado")]
        Expired = 7
    }

    public enum SignatureProvider
    {
        [Display(Name = "ZapSign")]
        ZapSign = 1,
        [Display(Name = "NeckSign")]
        NeckSign
    }

    public enum LogLevel
    {
        Info = 1,      // Informações gerais sobre a execução
        Warning,   // Problemas potenciais que não impedem a execução
        Error,     // Erros que causam falhas em operações
        Critical,  // Falhas graves que afetam a aplicação
        Debug,     // Informações detalhadas para depuração
        Trace,      // Detalhes muito finos de execução
        None
    }

    public enum TypeNotification
    {
        System = 1,     // Notificações geradas automaticamente pelo sistema (ex: relatório pronto)
        Alert,      // Avisos importantes que exigem atenção
        Message,    // Mensagens diretas para o usuário
        Task,      // Notificação de uma nova tarefa ou atribuição
        Informative,  // Informações gerais sem urgência
        None
    }

    public enum SnapIV
    {
        [Display(Name = "Nem um pouco")]
        NotAtAll = 1,
        [Display(Name = "Só um pouco")]
        JustALittle,
        [Display(Name = "Bastante")]
        QuiteALot,
        [Display(Name = "Demais")]
        TooMuch,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum SnapIVResult
    {
        [Display(Name = "Sem indícios de TDAH")]
        None = 1,
        [Display(Name = "TDAH Tipo Desatento")]
        Inattentive,
        [Display(Name = "TDAH Tipo Hiperativo/Impulsivo")]
        HyperactiveImpulsive,
        [Display(Name = "TDAH Combinado")]
        Combined
    }

    public enum StudentBehaviorTrait
    {
        [Display(Name = "Agressivo(a)")]
        Aggressive = 1,
        [Display(Name = "Passivo(a)")]
        Passive,
        [Display(Name = "Dependente")]
        Dependent,
        [Display(Name = "Medroso(a)")]
        Fearful,
        [Display(Name = "Retraído(a)")]
        Withdrawn,
        [Display(Name = "Melancólico(a)")]
        Melancholic,
        [Display(Name = "Calmo(a)")]
        Calm,
        [Display(Name = "Desligado(a)")]
        Distracted,
        [Display(Name = "Sem limites")]
        NoLimits,
        [Display(Name = "Agitado(a)")]
        Hyperactive,
        [Display(Name = "Depressivo(a)")]
        Depressive,
        [Display(Name = "Ressentido(a)")]
        Resentful,
        [Display(Name = "Outro")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum StudentDevelopmentComparison
    {
        [Display(Name = "Mais Infantil")]
        MoreImmature = 1,
        [Display(Name = "Na Média")]
        Average,
        [Display(Name = "Mais Maduro(a)")]
        MoreMature,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum Evaluation
    {
        [Display(Name = "Sim")]
        Yes = 1,
        [Display(Name = "Às Vezes")]
        Sometimes,
        [Display(Name = "Não")]
        Not,
        [Display(Name = "Nenhum(a)")]
        None,
        [Display(Name = "Outra")]
        Other
    }

    public enum AgeRange
    {
        //[Display(Name = "0 a 4 meses")]
        //ZeroFourMonths = 1,
        [Display(Name = "0 a 1 ano")]
        ZeroOneYear = 2,
        [Display(Name = "1 a 2 anos")]
        OneTwoYears,
        [Display(Name = "2 a 3 anos")]
        TwoThreeYears,
        [Display(Name = "3 a 4 anos")]
        ThreeFourYears,
        [Display(Name = "4 a 5 anos")]
        FourFiveYears,
        [Display(Name = "5 a 6 anos")]
        FiveSixYears,
        [Display(Name = "7 anos ou mais")]
        SevenOrMore,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum Area
    {
        [Display(Name = "Desenvolvimento Global")]
        GlobalDevelopment = 1,
        [Display(Name = "Socialização")]
        Socialization,
        [Display(Name = "Linguagem")]
        Language,
        [Display(Name = "Cognição")]
        Cognition,
        [Display(Name = "Autocuidado")]
        SelfCare,
        [Display(Name = "Desenvolvimento Motor")]
        MotorDevelopment,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum MaritalStatus
    {
        [Display(Name = "Casado")]
        Married = 1,
        [Display(Name = "Divorciado")]
        Divorced,
        [Display(Name = "Solteiro")]
        Single,
        [Display(Name = "Viúvo")]
        Widowed,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum SkinColor
    {
        [Display(Name = "Branco")]
        White = 1,
        [Display(Name = "Preto")]
        Black,
        [Display(Name = "Pardo")]
        Brown,
        [Display(Name = "Indígena")]
        Indigenous,
        [Display(Name = "Amarelo")]
        Yellow,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum PlanResponse
    {
        [Display(Name = "Ainda Não")]
        NotYet = 1,
        [Display(Name = "Com Mediação")]
        WithAssistence,
        [Display(Name = "Sozinho")]
        Alone,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum TypeAEE
    {
        [Display(Name = "Sala de Recursos Multifuncional")]
        MultifunctionalResourceRoom = 1,
        [Display(Name = "Intérprete na Sala Regular")]
        InterpreterInTheRegularClassroom,
        [Display(Name = "Professor de Libras")]
        LibrasTeacher,
        [Display(Name = "Tutor em Sala de Aula Regular")]
        TutorInTheRegularClassroom,
        [Display(Name = "Domiciliar")]
        Home,
        [Display(Name = "Hospitalar")]
        Hospital,
        [Display(Name = "Outro")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum ProfessionalsAEE
    {
        [Display(Name = "Fonoaudiologia")]
        SpeechLanguagePathology = 1,
        [Display(Name = "Psicologia")]
        Psychologist,
        [Display(Name = "Assistência Social")]
        SocialWork,
        [Display(Name = "Área Médica")]
        MedicalField,
        [Display(Name = "Outro")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum FrequencyAEE
    {
        [Display(Name = "2 Vezes por Semana")]
        TwoTimesPerWeek = 1,
        [Display(Name = "3 Vezes por Semana")]
        ThreeTimesPerWeek,
        [Display(Name = "4 Vezes por Semana")]
        FourTimesPerWeek,
        [Display(Name = "5 Vezes por Semana")]
        FiveTimesPerWeek,
        [Display(Name = "Todo o Período de Aula, na Própria Sala de Aula")]
        EntireClassPeriod,
        [Display(Name = "Outra")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum ServiceTimeAEE
    {
        [Display(Name = "60 Minutos por Atendimento")]
        FiftyMinutesPerSession = 1,
        [Display(Name = "Durante Todas as Aulas, na Própria Sala de Aula")]
        DuringAllClasses,
        [Display(Name = "Outro")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum WritingLevel
    {
        [Display(Name = "Alfabético")]
        Alphabetical = 1,
        [Display(Name = "Silábico-alfabético")]
        SyllabicAlphabetic,
        [Display(Name = "Silábico")]
        Syllabic,
        [Display(Name = "Pré-silábico")]
        PreSyllabic,
        [Display(Name = "Outro")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum ScaleDIResult
    {
        [Display(Name = "Sem atraso no desenvolvimento")]
        NoDevelopmentalDelay = 1,
        [Display(Name = "Atraso leve no desenvolvimento")]
        MildDevelopmentalDelay,
        [Display(Name = "Atraso moderado no desenvolvimento")]
        ModerateDevelopmentalDelay,
        [Display(Name = "Atraso grave no desenvolvimento")]
        SevereDevelopmentalDelay,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum SocialWorkerProcedures
    {
        [Display(Name = "Entrevista com a família")]
        FamilyInterview = 1,
        [Display(Name = "Visita domiciliar")]
        HomeVisit,
        [Display(Name = "Contato com a escola")]
        SchoolContact,
        [Display(Name = "Análise de documentos")]
        DocumentAnalysis,
        [Display(Name = "Consulta a outros serviços (saúde, assistência social)")]
        ConsultationWithOtherServices,
        [Display(Name = "Outro")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum FamilyComposition
    {
        [Display(Name = "Nuclear (pais e filhos)")]
        Nuclear = 1,
        [Display(Name = "Monoparental (mãe ou pai com filhos)")]
        SingleParent,
        [Display(Name = "Extensa (avós, tios, primos convivendo)")]
        Extended,
        [Display(Name = "Outro")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum NumberOfPeopleInTheHousehold
    {
        [Display(Name = "2 a 3 pessoas")]
        TwoToThreePeople = 1,
        [Display(Name = "4 a 5 pessoas")]
        FourToFivePeople,
        [Display(Name = "6 ou mais pessoas")]
        SixOrMorePeople,
        [Display(Name = "Outro")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum HousingType
    {
        [Display(Name = "Própria")]
        Owned = 1,
        [Display(Name = "Alugada")]
        Rented,
        [Display(Name = "Cedida (por familiares ou outros)")]
        ProvidedByFamilyOrOthers,
        [Display(Name = "Outra")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum HousingCondition
    {
        [Display(Name = "Adequada (estrutura segura, água, luz, esgoto)")]
        Adequate = 1,
        [Display(Name = "Precária (problemas estruturais ou falta de saneamento)")]
        Precarious,
        [Display(Name = "Insalubre (risco à saúde)")]
        Unhealthy,
        [Display(Name = "Outra")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum NumberOfHouseholdRooms
    {
        [Display(Name = "1 a 2 cômodos")]
        OneToTwoRooms = 1,
        [Display(Name = "3 a 4 cômodos")]
        ThreeToFourRooms,
        [Display(Name = "5 ou mais cômodos")]
        FiveOrMoreRooms,
        [Display(Name = "Outro")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum MonthlyFamilyIncome
    {
        [Display(Name = "Até 1 salário mínimo")]
        UpToOneMinimumWage = 1,
        [Display(Name = "De 1 a 2 salários mínimos")]
        FromOneToTwoMinimumWages,
        [Display(Name = "De 2 a 3 salários mínimos")]
        FromTwoToThreeMinimumWages,
        [Display(Name = "Acima de 3 salários mínimos")]
        AboveThreeMinimumWages,
        [Display(Name = "Outra")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum IncomeSource
    {
        [Display(Name = "Trabalho formal (carteira assinada)")]
        FormalEmployment = 1,
        [Display(Name = "Trabalho informal")]
        InformalEmployment,
        [Display(Name = "Autônomo")]
        SelfEmployed,
        [Display(Name = "Benefícios sociais")]
        SocialBenefits,
        [Display(Name = "Desempregado")]
        Unemployed,
        [Display(Name = "Outra")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum GovernmentBenefits
    {
        [Display(Name = "Bolsa Família")]
        BolsaFamilia = 1,
        [Display(Name = "BPC (Benefício de Prestação Continuada)")]
        BPC,
        [Display(Name = "Cesta básica")]
        BasicFoodBasket,
        [Display(Name = "Nenhum")]
        None,
        [Display(Name = "Outro")]
        Other
    }

    public enum HealthConditions
    {
        [Display(Name = "Não")]
        None = 1,
        [Display(Name = "Doença crônica (diabetes, asma, etc.)")]
        ChronicDisease,
        [Display(Name = "Deficiência física ou intelectual")]
        PhysicalOrIntellectualDisability,
        [Display(Name = "Transtorno mental ou emocional")]
        MentalOrEmotionalDisorder,
        [Display(Name = "Outra")]
        Other
    }

    public enum HealthServiceAccess
    {
        [Display(Name = "Regular (UBS, posto de saúde)")]
        Regular = 1,
        [Display(Name = "Difícil (longe, falta transporte)")]
        Difficult,
        [Display(Name = "Privado (plano de saúde)")]
        Private,
        [Display(Name = "Outro")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum SchoolAttendance
    {
        [Display(Name = "Regular")]
        Regular = 1,
        [Display(Name = "Irregular (faltas frequentes)")]
        Irregular,
        [Display(Name = "Evadido")]
        DroppedOut,
        [Display(Name = "Outro")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum AcademicPerformance
    {
        [Display(Name = "Satisfatório")]
        Satisfactory = 1,
        [Display(Name = "Com dificuldades")]
        WithDifficulties,
        [Display(Name = "Necessita de acompanhamento especializado")]
        RequiresSpecializedSupport,
        [Display(Name = "Outra")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum MainSchoolDifficulties
    {
        [Display(Name = "Leitura e escrita")]
        ReadingAndWriting = 1,
        [Display(Name = "Matemática")]
        Math,
        [Display(Name = "Concentração")]
        Concentration,
        [Display(Name = "Comportamento")]
        Behavior,
        [Display(Name = "Nenhuma")]
        None,
        [Display(Name = "Outra")]
        Other
    }

    public enum DailyMeals
    {
        [Display(Name = "3 ou mais refeições")]
        ThreeOrMore = 1,
        [Display(Name = "2 refeições")]
        Two,
        [Display(Name = "1 refeição ou menos")]
        OneOrLess,
        [Display(Name = "Outro")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum FoodSecurity
    {
        [Display(Name = "Alimentação regular")]
        Regular = 1,
        [Display(Name = "Insegurança alimentar (falta de alimentos frequente)")]
        FoodInsecurity,
        [Display(Name = "Depende de programas sociais (merenda escolar, cestas básicas)")]
        DependsOnSocialPrograms,
        [Display(Name = "Outra")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum FamilyRelationship
    {
        [Display(Name = "Harmonioso")]
        Harmonious = 1,
        [Display(Name = "Com conflitos frequentes")]
        FrequentConflicts,
        [Display(Name = "Indícios de violência doméstica")]
        DomesticViolenceIndicators,
        [Display(Name = "Outro")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum SocialSupportNetwork
    {
        [Display(Name = "Presente (familiares, vizinhos, amigos)")]
        Present = 1,
        [Display(Name = "Frágil ou inexistente")]
        WeakOrNonexistent,
        [Display(Name = "Outro")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum CommunityActivityParticipation
    {
        [Display(Name = "Ativa (igreja, esportes, projetos sociais)")]
        Active = 1,
        [Display(Name = "Pouca ou nenhuma participação")]
        LowOrNone,
        [Display(Name = "Outra")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum Recommendations
    {
        [Display(Name = "Acompanhamento psicológico")]
        PsychologicalSupport = 1,
        [Display(Name = "Encaminhamento para serviços de saúde")]
        HealthServiceReferral,
        [Display(Name = "Apoio material (cestas básicas, auxílios)")]
        MaterialSupport,
        [Display(Name = "Orientação familiar")]
        FamilyGuidance,
        [Display(Name = "Acompanhamento escolar especializado")]
        SpecializedSchoolSupport,
        [Display(Name = "Inclusão em programas sociais")]
        SocialProgramsInclusion,
        [Display(Name = "Participação em atividades comunitárias")]
        CommunityActivityParticipation,
        [Display(Name = "Outra")]
        Other,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum TypeOfDisability
    {
        [Display(Name = "TEA (Transtorno do Espectro Autista)")]
        TEA = 1,
        [Display(Name = "Deficiência Intelectual")]
        Intellectual,
        [Display(Name = "Deficiência Física")]
        Physical,
        [Display(Name = "Surdez")]
        Deafness,
        [Display(Name = "Baixa Visão")]
        LowVision,
        [Display(Name = "Deficiências Múltiplas")]
        Multiple,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum TypeOfDisorder
    {
        [Display(Name = "Dislexia")]
        Dyslexia = 1,
        [Display(Name = "Disgrafia")]
        Dysgraphia,
        [Display(Name = "Disortografia")]
        Dysorthographia,
        [Display(Name = "Discalculia")]
        Dyscalculia,
        [Display(Name = "Apraxia de Fala")]
        SpeechApraxia,
        [Display(Name = "TDAH (Transtorno de Déficit de Atenção e Hiperatividade)")]
        ADHD,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum PsychopedagogueRecommendations
    {
        [Display(Name = "Encaminhamento para avaliação médica especializada")]
        MedicalEvaluation = 1,
        [Display(Name = "Acompanhamento psicopedagógico regular")]
        RegularPsychopedagogicalFollowUp,
        [Display(Name = "Adaptações curriculares necessárias")]
        CurriculumAdaptations,
        [Display(Name = "Recursos de tecnologia assistiva")]
        AssistiveTechnologyResources,
        [Display(Name = "Apoio de professor auxiliar/mediador")]
        TeacherSupport,
        [Display(Name = "Atividades diferenciadas em sala de aula")]
        DifferentiatedClassroomActivities,
        [Display(Name = "Mais tempo para realização de atividades e avaliações")]
        ExtraTimeForTasks,
        [Display(Name = "Material pedagógico adaptado")]
        AdaptedTeachingMaterials,
        [Display(Name = "Comunicação alternativa")]
        AlternativeCommunication,
        [Display(Name = "Trabalho colaborativo com equipe multidisciplinar")]
        MultidisciplinaryTeamCollaboration,
        [Display(Name = "Orientação familiar")]
        FamilyGuidance,
        [Display(Name = "Elaboração de PEI (Plano Educacional Individualizado)")]
        IndividualizedEducationPlan,
        [Display(Name = "Elaboração de PAEE (Plano de Atendimento Educacional Especializado)")]
        SpecializedEducationalPlan,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum MCHATResult
    {
        [Display(Name = "Baixo Risco")]
        LowRisk = 1,
        [Display(Name = "Risco Médio")]
        ModerateRisk,
        [Display(Name = "Risco Elevado")]
        HighRisk,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum CARSResult
    {
        [Display(Name = "Sem Autismo")]
        NoAutism = 1,
        [Display(Name = "Autismo Leve a Moderado")]
        MildToModerateAutism,
        [Display(Name = "Autismo Moderado a Severo")]
        SevereAutism,
        [Display(Name = "Nenhum(a)")]
        None
    }

    public enum Subject
    {   
        [Display(Name = "Língua Portuguesa")]
        Portuguese = 1,
        [Display(Name = "Matemática")]
        Mathematics,
        [Display(Name = "Ciências")]
        Science,
        [Display(Name = "História")]
        History,
        [Display(Name = "Geografia")]
        Geography,
        [Display(Name = "Inglês")]
        English,
        [Display(Name = "Nenhum(a)")]
        None,
        [Display(Name = "Educação Infantil")]
        EarlyChildhoodEducation = 8
    }

    public enum CognitiveResult
    {
        [Display(Name = "Desenvolvimento cognitivo dentro do esperado")]
        WithinExpected = 1,
        [Display(Name = "Desenvolvimento cognitivo limítrofe")]
        Borderline,
        [Display(Name = "Sugestivo de limitação intelectual leve")]
        MildIntellectualDisability,
        [Display(Name = "Sugestivo de limitação intelectual moderada a severa")]
        ModerateToSevereIntellectualDisability,
        [Display(Name = "Nenhum")]
        None
    }
}
