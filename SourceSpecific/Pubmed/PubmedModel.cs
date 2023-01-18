using PostgreSQLCopyHelper;
namespace MDR_Harvester.Pubmed;

public class Pubmed_Record
{
    public string? sd_oid { get; set; }
    public int? ipmid { get; set; }
    public int? pmid_version { get; set; }

    public NumericDate? dateCitationCompleted { get; set; }
    public NumericDate? dateCitationRevised { get; set; }
    public string? status { get; set; }
    public string? owner { get; set; }
    public int? versionID { get; set; }

    public string? articleTitle { get; set; }
    public string? vernacularTitle { get; set; }

    public int? pubYear { get; set; }
    public string? pubMonth { get; set; }
    public int? pubDay { get; set; }
    public string? medlineDate { get; set; }
    public string? pubModel { get; set; }

    public string? journalIssnType { get; set; }
    public string? journalIssn { get; set; }
    public string? journalVolume { get; set; }
    public string? journalIssue { get; set; }
    public string? journalCitedMedium { get; set; }
    public string? journalTitle { get; set; }
    public string? journalISOAbbreviation { get; set; }
    public string? medlinePgn { get; set; }

    public string? journalCountry { get; set; }
    public string? journalMedlineTA { get; set; }
    public string? journalNlmUniqueID { get; set; }
    public string? journalISSNLinking { get; set; }

    public string? PublicationStatus { get; set; }

    public List<string>? ArticleLangs { get; set; }
    public List<Creator>? Creators { get; set; }
    public List<ArticleEDate>? ArticleEDates { get; set; }
    public List<ArticleType>? ArticleTypes { get; set; }
    public List<EReference>? EReferences { get; set; }
    public List<Database>? DatabaseList { get; set; }
    public List<Fund>? FundingList { get; set; }
    public List<Substance>? SubstanceList { get; set; }
    public List<MeshTerm>? MeshList { get; set; }
    public List<SupplMeshTerm>? SupplMeshList { get; set; }

    public string? keywordOwner { get; set; }
    public List<KWord>? KeywordList { get; set; }

    public List<Correction>? CorrectionsList { get; set; }
    public List<AdditionalId>? AdditionalIds { get; set; }
    public List<HistoryDate>? History { get; set; }
    public List<ArticleId>? ArticleIds { get; set; }
    public List<ISSNRecord>? ISSNList { get; set; }
}

public class NumericDate
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }

    public NumericDate(int? year, int? month, int? day)
    {
        Year = year;
        Month = month;
        Day = day;
    }
}

public class EReference
{
    public string? EIdType { get; set; }
    public string? Value { get; set; }

    public EReference(string? eIdType, string? value)
    {
        EIdType = eIdType;
        Value = value;
    }
}


public class Creator
{
    public string? CollectiveName { get; set; }
    public string? FamilyName { get; set; }
    public string? GiveneName { get; set; }
    public string? Initials { get; set; }
    public string? Suffix { get; set; }
    public string? IdentifierSource { get; set; }
    public string? IdentifierValue { get; set; }
    public List<AffiliationInfo>? AffiliationInfo { get; set; }

    public Creator(string? collectiveName, string? familyName,
                   string? giveneName, string? initials,
                   string? suffix, string? identifierSource,
                   string? identifierValue, List<AffiliationInfo>? affiliationInfo)
    {
        CollectiveName = collectiveName;
        FamilyName = familyName;
        GiveneName = giveneName;
        Initials = initials;
        Suffix = suffix;
        IdentifierSource = identifierSource;
        IdentifierValue = identifierValue;
        AffiliationInfo = affiliationInfo;
    }
}


public class AffiliationInfo
{
    public string? Affiliation { get; set; }
    public string? IdentifierSource { get; set; }
    public string? IdentifierValue { get; set; }

    public AffiliationInfo(string? affiliation, string? identifierSource,
                           string? identifierValue)
    {
        Affiliation = affiliation;
        IdentifierSource = identifierSource;
        IdentifierValue = identifierValue;
    }
}


public class AdditionalId
{
    public string? Source { get; set; }
    public string? Value { get; set; }

    public AdditionalId(string? source, string? value)
    {
        Source = source;
        Value = value;
    }
}


public class ArticleEDate
{
    public string? DateType { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }

    public ArticleEDate(string? dateType, int? year, int? month, int? day)
    {
        DateType = dateType;
        Year = year;
        Month = month;
        Day = day;
    }
}


public class Database
{
    public string? DataBankName { get; set; }
    public List<string>? AccessionNumberList { get; set; }

    public Database(string? dataBankName, List<string>? accessionNumberList)
    {
        DataBankName = dataBankName;
        AccessionNumberList = accessionNumberList;
    }
}

public class Fund
{
    public string? GrantID { get; set; }
    public string? Acronym { get; set; }
    public string? Agency { get; set; }
    public string? Country { get; set; }

    public Fund(string? grantID, string? acronym,
                string? agency, string? country)
    {
        GrantID = grantID;
        Acronym = acronym;
        Agency = agency;
        Country = country;
    }
}

public class ArticleType
{
    public string? UI { get; set; }
    public string? Value { get; set; }

    public ArticleType(string? uI, string? value)
    {
        UI = uI;
        Value = value;
    }
}

public class Substance
{
    public string? UI { get; set; }
    public string? Name { get; set; }

    public Substance(string? uI, string? name)
    {
        UI = uI;
        Name = name;
    }
}

public class SupplMeshTerm
{
    public string? Type { get; set; }
    public string? UI { get; set; }
    public string? Value { get; set; }

    public SupplMeshTerm(string? type, string? uI, string? value)
    {
        Type = type;
        UI = uI;
        Value = value;
    }
}

public class MeshTerm
{
    public string? UI { get; set; }
    public string? MajorTopicYN { get; set; }
    public string? Type { get; set; }
    public string? Value { get; set; }

    public MeshTerm(string? uI, string? majorTopicYN,
                    string? type, string? value)
    {
        UI = uI;
        MajorTopicYN = majorTopicYN;
        Type = type;
        Value = value;
    }
}

public class KWord
{
    public string? MajorTopicYN { get; set; }
    public string? Value { get; set; }

    public KWord(string? majorTopicYN, string? value)
    {
        MajorTopicYN = majorTopicYN;
        Value = value;
    }
}

public class Correction
{
    public string? RefSource { get; set; }
    public int? PMID_Version { get; set; }
    public int? PMID_Value { get; set; }
    public string? RefType { get; set; }
    public string? Note { get; set; }

    public Correction(string? refSource, int? pMID_Version,
                      int? pMID_Value, string? refType, string note)
    {
        RefSource = refSource;
        PMID_Version = pMID_Version;
        PMID_Value = pMID_Value;
        RefType = refType;
        Note = note;
    }
}

public class HistoryDate
{
    public string PubStatus { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }

    public HistoryDate(int? year, int? month, int? day, string pubStatus)
    {
        PubStatus = pubStatus;
        Year = year;
        Month = month;
        Day = day;
    }
}

public class ArticleId
{
    public string? IdType { get; set; }
    public string? Value { get; set; }

    public ArticleId(string? idType, string? value)
    {
        IdType = idType;
        Value = value;
    }
}


public class ISSNRecord
{
    public string? IssnType { get; set; }
    public string? Value { get; set; }

    public ISSNRecord(string? issnType, string? value)
    {
        IssnType = issnType;
        Value = value;
    }
}


public class PMSource
{
    public int? id { get; set; }
    public string? default_name { get; set; }
    public string? nlm_abbrev { get; set; }
}


public class PMIDBySource
{
    public string? sd_sid { get; set; }
    public string? pmid { get; set; }
}

/*
public class PMIDByBank
{
    public string pmid { get; set; }

    public PMIDByBank( string _pmid)
    {
        pmid = _pmid;
    }
}
*/

public class CopyHelpers
{
    // defines the copy helpers required.
    // see https://githur.com/PostgreSQLCopyHelper/PostgreSQLCopyHelper for details

    public PostgreSQLCopyHelper<PMIDBySource> source_ids_helper =
            new PostgreSQLCopyHelper<PMIDBySource>("pp", "pmids_by_source_total")
                .MapVarchar("sd_sid", x => x.sd_sid)
                .MapVarchar("pmid", x => x.pmid);

    // public PostgreSQLCopyHelper<PMIDByBank> bank_ids_helper =
    //     new PostgreSQLCopyHelper<PMIDByBank>("pp", "temp_pmids_by_bank")
    //           .MapVarchar("pmid", x => x.pmid);
}