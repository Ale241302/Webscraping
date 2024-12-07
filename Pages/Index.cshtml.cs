using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using OfficeOpenXml;
using WebScraping.Models;
using Npgsql;
using OpenQA.Selenium.BiDi.Modules.Script;

namespace WebScraping.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string Message { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SelectedCountry { get; set; }
        public DateTime? LastScrapingDate { get; set; }
        public string SelectedCountryName { get; set; }

        public List<SelectListItem> CountryList { get; set; }

        public bool HasRecords { get; set; }  // Bandera para habilitar el botón de exportar
        public List<Company> CompanyList { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public const int PageSize = 10;
        public int RecordCount { get; set; }
        public int DailyQuotaRemaining { get; set; } = 600;

        [BindProperty(SupportsGet = true)]
        public string Source { get; set; }

        public void OnGet()
        {
            LoadCountryList();
            // Establecer un valor predeterminado si Source está vacío
            if (string.IsNullOrEmpty(Source))
            {
                Source = "WCA"; // Valor predeterminado
            }

            if (!string.IsNullOrEmpty(SelectedCountry))
            {
                // Convertir SelectedCountry a su ID numérico si Source es "GLA"
                if (Source == "GLA" && countryIdMap.ContainsKey(SelectedCountry))
                {
                    SelectedCountry = countryIdMap[SelectedCountry]; // Convertir a ID numérico
                }
                else if (Source == "JCTRANS" && countryIdMap2.ContainsKey(SelectedCountry))
                {
                    SelectedCountry = countryIdMap2[SelectedCountry];
                }
                else if (Source == "DF" && countryIdMap3.ContainsKey(SelectedCountry))
                {
                    SelectedCountry = countryIdMap3[SelectedCountry];
                }

                HasRecords = CheckIfRecordsExist(SelectedCountry, Source);

                if (HasRecords)
                {
                    CompanyList = GetCompaniesByCountry(SelectedCountry, Source); // Cargar los registros de empresas para el país
                    LastScrapingDate = GetLastScrapingDate(SelectedCountry, Source); // Obtener la última fecha de scraping
                    RecordCount = GetRecordCountByCountry(SelectedCountry, Source); // Asignar el conteo
                    DailyQuotaRemaining = GetDailyQuotaRemaining(SelectedCountry, Source); // Asignar el contador diario
                }

                // Obtener el nombre completo del país seleccionado
                if (Source == "GLA")
                {
                    // Buscar la clave en el diccionario para obtener el código alfabético
                    var alphaCode = countryIdMap.FirstOrDefault(x => x.Value == SelectedCountry).Key;
                    SelectedCountryName = CountryList.FirstOrDefault(c => c.Value == alphaCode)?.Text;
                }
                else if (Source == "JCTRANS")
                {
                    // Buscar la clave en el diccionario para obtener el código alfabético
                    var alphaCode = countryIdMap2.FirstOrDefault(x => x.Value == SelectedCountry).Key;
                    SelectedCountryName = CountryList.FirstOrDefault(c => c.Value == alphaCode)?.Text;
                }
                else if (Source == "DF")
                {
                    // Buscar la clave en el diccionario para obtener el código alfabético
                    var alphaCode = countryIdMap3.FirstOrDefault(x => x.Value == SelectedCountry).Key;
                    SelectedCountryName = CountryList.FirstOrDefault(c => c.Value == alphaCode)?.Text;
                }
                else
                {
                    SelectedCountryName = CountryList.FirstOrDefault(c => c.Value == SelectedCountry)?.Text;
                }
            }
        }


        private Dictionary<string, string> countryIdMap = new Dictionary<string, string>
        {
            {"AF", "1"}, {"AL", "2"}, {"DZ", "3"}, {"AS", "4"}, {"AD", "5"},
            {"AO", "6"}, {"AI", "7"}, {"AQ", "8"}, {"AG", "9"}, {"AR", "10"},
            {"AM", "11"}, {"AW", "12"}, {"AU", "13"}, {"AT", "14"}, {"AZ", "15"},
            {"BS", "16"}, {"BH", "17"}, {"BD", "18"}, {"BB", "19"}, {"BY", "20"},
            {"BE", "21"}, {"BZ", "22"}, {"BJ", "23"}, {"BM", "24"}, {"BT", "25"},
            {"BO", "26"}, {"BA", "27"}, {"BW", "28"}, {"BV", "29"}, {"BR", "30"},
            {"VG", "31"}, {"BN", "32"}, {"BG", "33"}, {"BF", "34"}, {"BI", "35"},
            {"KH", "36"}, {"CM", "37"}, {"CA", "38"}, {"CV", "39"}, {"BQ", "150"},
            {"KY", "40"}, {"CF", "41"}, {"TD", "42"}, {"CL", "43"}, {"CN", "44"},
            {"CX", "45"}, {"CC", "46"}, {"CO", "47"}, {"KM", "48"}, {"CD", "49"},
            {"CG", "50"}, {"CK", "51"}, {"CR", "52"}, {"CI", "53"}, {"HR", "54"},
            {"CU", "55"}, {"CY", "56"}, {"CZ", "57"}, {"DK", "58"}, {"DJ", "59"},
            {"DM", "60"}, {"DO", "61"}, {"TL", "62"}, {"EC", "63"}, {"EG", "64"},
            {"SV", "65"}, {"GQ", "66"}, {"ER", "67"}, {"EE", "68"}, {"ET", "69"},
            {"FK", "70"}, {"FO", "71"}, {"FJ", "72"}, {"FI", "73"}, {"FR", "74"},
            {"GF", "75"}, {"PF", "76"}, {"GA", "77"}, {"GM", "78"}, {"GE", "79"},
            {"DE", "80"}, {"GH", "81"}, {"GI", "82"}, {"GR", "83"}, {"GL", "84"},
            {"GD", "85"}, {"GP", "86"}, {"GU", "87"}, {"GT", "88"}, {"GN", "89"},
            {"GW", "90"}, {"GY", "91"}, {"HT", "92"}, {"HM", "94"}, {"HN", "93"},
            {"HU", "95"}, {"IS", "96"}, {"IN", "97"}, {"ID", "98"}, {"IR", "99"},
            {"IQ", "100"}, {"IE", "101"}, {"IL", "102"}, {"IT", "103"}, {"JM", "104"},
            {"JP", "105"}, {"JO", "106"}, {"KZ", "107"}, {"KE", "108"}, {"KI", "109"},
            {"KR", "111"}, {"KW", "112"}, {"KG", "113"}, {"LA", "114"}, {"LV", "115"},
            {"LB", "116"}, {"LS", "117"}, {"LR", "118"}, {"LY", "119"}, {"LI", "120"},
            {"LT", "121"}, {"LU", "122"}, {"MK", "124"}, {"MG", "125"}, {"MW", "126"},
            {"MY", "127"}, {"MV", "128"}, {"ML", "129"}, {"MT", "130"}, {"IM", "123"},
            {"MH", "131"}, {"MQ", "132"}, {"MR", "133"}, {"MU", "134"}, {"YT", "135"},
            {"MX", "136"}, {"FM", "137"}, {"MD", "138"}, {"MC", "139"}, {"MN", "140"},
            {"ME", "141"}, {"MS", "142"}, {"MA", "143"}, {"MZ", "144"}, {"MM", "145"},
            {"NA", "146"}, {"NR", "147"}, {"NP", "148"}, {"NL", "149"}, {"NC", "151"},
            {"NZ", "152"}, {"NI", "153"}, {"NE", "154"}, {"NG", "155"}, {"NU", "156"},
            {"NF", "157"}, {"MP", "158"}, {"NO", "159"}, {"OM", "160"}, {"PK", "161"},
            {"PW", "162"}, {"PS", "3163"}, {"PA", "163"}, {"PG", "164"}, {"PY", "165"},
            {"PE", "166"}, {"PH", "167"}, {"PN", "168"}, {"PL", "169"}, {"PT", "170"},
            {"PR", "171"}, {"QA", "172"}, {"RE", "173"}, {"RO", "174"}, {"RU", "175"},
            {"RW", "176"}, {"SH", "177"}, {"KN", "178"}, {"LC", "179"}, {"VC", "197"},
            {"SM", "180"}, {"ST", "181"}, {"SA", "182"}, {"SN", "183"}, {"RS", "184"},
            {"SC", "185"}, {"SL", "186"}, {"SG", "187"}, {"SX", "188"}, {"SK", "189"},
            {"SI", "190"}, {"SB", "191"}, {"SO", "192"}, {"ZA", "193"}, {"SS", "194"},
            {"ES", "195"}, {"LK", "196"}, {"SD", "198"}, {"SR", "199"}, {"SJ", "200"},
            {"SZ", "201"}, {"SE", "202"}, {"CH", "203"}, {"SY", "204"}, {"TJ", "206"},
            {"TZ", "207"}, {"TH", "208"}, {"TG", "209"}, {"TK", "210"}, {"TO", "211"},
            {"TT", "212"}, {"TN", "213"}, {"TR", "214"}, {"TM", "215"}, {"TC", "216"},
            {"TV", "217"}, {"AE", "220"}, {"UG", "218"}, {"GB", "221"}, {"UA", "219"},
            {"UY", "223"}, {"US", "222"}, {"UZ", "224"}, {"VU", "225"}, {"VE", "226"},
            {"VN", "227"}, {"VI", "228"}, {"WF", "229"}, {"EH", "230"}, {"WS", "231"},
            {"YE", "232"}, {"ZM", "233"}, {"ZW", "234"}
        };

        private Dictionary<string, string> countryIdMap2 = new Dictionary<string, string>
        {
             {"AF", "Afghanistan"}, {"AL", "Albania"}, {"DZ", "Algeria"}, {"AS", "American Samoa"}, {"AD", "Andorra"},
            {"AO", "Angola"}, {"AI", "Anguilla"}, {"AQ", "Antarctica"}, {"AG", "Antigua and Barbuda"}, {"AR", "Argentina"},
            {"AM", "Armenia"}, {"AW", "Aruba"}, {"AU", "Australia"}, {"AT", "Austria"}, {"AZ", "Azerbaijan"},
            {"BS", "Bahamas"}, {"BH", "Bahrain"}, {"BD", "Bangladesh"}, {"BB", "Barbados"}, {"BY", "Belarus"},
            {"BE", "Belgium"}, {"BZ", "Belize"}, {"BJ", "Benin"}, {"BM", "Bermuda"}, {"BT", "Bhutan"},
            {"BO", "Bolivia"}, {"BA", "Bosnia and Herzegovina"}, {"BW", "Botswana"}, {"BV", "Bouvet Island"}, {"BR", "Brazil"},
            {"VG", "British Virgin Islands"}, {"BN", "Brunei"}, {"BG", "Bulgaria"}, {"BF", "Burkina Faso"}, {"BI", "Burundi"},
            {"KH", "Cambodia"}, {"CM", "Cameroon"}, {"CA", "Canada"}, {"CV", "Cape Verde"}, {"BQ", "Bonaire"},
            {"KY", "Cayman Islands"}, {"CF", "Central African Republic"}, {"TD", "Chad"}, {"CL", "Chile"}, {"CN", "China"},
            {"CX", "Christmas Island"}, {"CC", "Cocos Islands"}, {"CO", "Colombia"}, {"KM", "Comoros"}, {"CD", "Congo"},
            {"CG", "Congo, Republic of"}, {"CK", "Cook Islands"}, {"CR", "Costa Rica"}, {"CI", "Côte d'Ivoire"}, {"HR", "Croatia"},
            {"CU", "Cuba"}, {"CY", "Cyprus"}, {"CZ", "Czech Republic"}, {"DK", "Denmark"}, {"DJ", "Djibouti"},
            {"DM", "Dominica"}, {"DO", "Dominican Republic"}, {"TL", "East Timor"}, {"EC", "Ecuador"}, {"EG", "Egypt"},
            {"SV", "El Salvador"}, {"GQ", "Equatorial Guinea"}, {"ER", "Eritrea"}, {"EE", "Estonia"}, {"ET", "Ethiopia"},
            {"FK", "Falkland Islands"}, {"FO", "Faroe Islands"}, {"FJ", "Fiji"}, {"FI", "Finland"}, {"FR", "France"},
            {"GF", "French Guiana"}, {"PF", "French Polynesia"}, {"GA", "Gabon"}, {"GM", "Gambia"}, {"GE", "Georgia"},
            {"DE", "Germany"}, {"GH", "Ghana"}, {"GI", "Gibraltar"}, {"GR", "Greece"}, {"GL", "Greenland"},
            {"GD", "Grenada"}, {"GP", "Guadeloupe"}, {"GU", "Guam"}, {"GT", "Guatemala"}, {"GN", "Guinea"},
            {"GW", "Guinea-Bissau"}, {"GY", "Guyana"}, {"HT", "Haiti"}, {"HM", "Heard Island and McDonald Islands"}, {"HN", "Honduras"},
            {"HU", "Hungary"}, {"IS", "Iceland"}, {"IN", "India"}, {"ID", "Indonesia"}, {"IR", "Iran"},
            {"IQ", "Iraq"}, {"IE", "Ireland"}, {"IL", "Israel"}, {"IT", "Italy"}, {"JM", "Jamaica"},
            {"JP", "Japan"}, {"JO", "Jordan"}, {"KZ", "Kazakhstan"}, {"KE", "Kenya"}, {"KI", "Kiribati"},
            {"KR", "South Korea"}, {"KW", "Kuwait"}, {"KG", "Kyrgyzstan"}, {"LA", "Laos"}, {"LV", "Latvia"},
            {"LB", "Lebanon"}, {"LS", "Lesotho"}, {"LR", "Liberia"}, {"LY", "Libya"}, {"LI", "Liechtenstein"},
            {"LT", "Lithuania"}, {"LU", "Luxembourg"}, {"MK", "Macedonia"}, {"MG", "Madagascar"}, {"MW", "Malawi"},
            {"MY", "Malaysia"}, {"MV", "Maldives"}, {"ML", "Mali"}, {"MT", "Malta"}, {"IM", "Isle of Man"},
            {"MH", "Marshall Islands"}, {"MQ", "Martinique"}, {"MR", "Mauritania"}, {"MU", "Mauritius"}, {"YT", "Mayotte"},
            {"MX", "Mexico"}, {"FM", "Micronesia"}, {"MD", "Moldova"}, {"MC", "Monaco"}, {"MN", "Mongolia"},
            {"ME", "Montenegro"}, {"MS", "Montserrat"}, {"MA", "Morocco"}, {"MZ", "Mozambique"}, {"MM", "Myanmar"},
            {"NA", "Namibia"}, {"NR", "Nauru"}, {"NP", "Nepal"}, {"NL", "Netherlands"}, {"NC", "New Caledonia"},
            {"NZ", "New Zealand"}, {"NI", "Nicaragua"}, {"NE", "Niger"}, {"NG", "Nigeria"}, {"NU", "Niue"},
            {"NF", "Norfolk Island"}, {"MP", "Northern Mariana Islands"}, {"NO", "Norway"}, {"OM", "Oman"}, {"PK", "Pakistan"},
            {"PW", "Palau"}, {"PS", "Palestine"}, {"PA", "Panama"}, {"PG", "Papua New Guinea"}, {"PY", "Paraguay"},
            {"PE", "Peru"}, {"PH", "Philippines"}, {"PN", "Pitcairn"}, {"PL", "Poland"}, {"PT", "Portugal"},
            {"PR", "Puerto Rico"}, {"QA", "Qatar"}, {"RE", "Réunion"}, {"RO", "Romania"}, {"RU", "Russia"},
            {"RW", "Rwanda"}, {"SH", "Saint Helena"}, {"KN", "Saint Kitts and Nevis"}, {"LC", "Saint Lucia"}, {"VC", "Saint Vincent and the Grenadines"},
            {"SM", "San Marino"}, {"ST", "São Tomé and Príncipe"}, {"SA", "Saudi Arabia"}, {"SN", "Senegal"}, {"RS", "Serbia"},
            {"SC", "Seychelles"}, {"SL", "Sierra Leone"}, {"SG", "Singapore"}, {"SX", "Sint Maarten"}, {"SK", "Slovakia"},
            {"SI", "Slovenia"}, {"SB", "Solomon Islands"}, {"SO", "Somalia"}, {"ZA", "South Africa"}, {"SS", "South Sudan"},
            {"ES", "Spain"}, {"LK", "Sri Lanka"}, {"SD", "Sudan"}, {"SR", "Suriname"}, {"SJ", "Svalbard and Jan Mayen"},
            {"SZ", "Eswatini"}, {"SE", "Sweden"}, {"CH", "Switzerland"}, {"SY", "Syria"}, {"TJ", "Tajikistan"},
            {"TZ", "Tanzania"}, {"TH", "Thailand"}, {"TG", "Togo"}, {"TK", "Tokelau"}, {"TO", "Tonga"},
            {"TT", "Trinidad and Tobago"}, {"TN", "Tunisia"}, {"TR", "Turkey"}, {"TM", "Turkmenistan"}, {"TC", "Turks and Caicos Islands"},
            {"TV", "Tuvalu"}, {"AE", "United Arab Emirates"}, {"UG", "Uganda"}, {"GB", "United Kingdom"}, {"UA", "Ukraine"},
            {"UY", "Uruguay"}, {"US", "United States"}, {"UZ", "Uzbekistan"}, {"VU", "Vanuatu"}, {"VE", "Venezuela"},
            {"VN", "Vietnam"}, {"VI", "Virgin Islands (U.S.)"}, {"WF", "Wallis and Futuna"}, {"EH", "Western Sahara"}, {"WS", "Samoa"},
            {"YE", "Yemen"}, {"ZM", "Zambia"}, {"ZW", "Zimbabwe"}
        };
        private Dictionary<string, string> countryIdMap3 = new Dictionary<string, string>
        {
                {"AF", "Afghanistan|0|AF"},
                {"AL", "Albania|1|AL"},
                {"DZ", "Algeria|2|DZ"},
                {"AS", "American Samoa|3|AS"},
                {"AD", "Andorra|4|AD"},
                {"AO", "Angola|5|AO"},
                {"AR", "Argentina|6|AR"},
                {"AW", "Aruba|7|AW"},
                {"AU", "Australia|8|AU"},
                {"AT", "Austria|9|AT"},
                {"AZ", "Azerbaijan|10|AZ"},
                {"BS", "Bahamas|11|BS"},
                {"BH", "Bahrain|12|BH"},
                {"BD", "Bangladesh|13|BD"},
                {"BB", "Barbados|14|BB"},
                {"BY", "Belarus|15|BY"},
                {"BE", "Belgium|16|BE"},
                {"BZ", "Belize|17|BZ"},
                {"BJ", "Benin|18|BJ"},
                {"BM", "Bermuda|19|BM"},
                {"BT", "Bhutan|20|BT"},
                {"BO", "Bolivia|21|BO"},
                {"BA", "Bosnia and Herzegovina|22|BA"},
                {"BW", "Botswana|23|BW"},
                {"BR", "Brazil|24|BR"},
                {"BN", "Brunei|25|BN"},
                {"BG", "Bulgaria|26|BG"},
                {"BF", "Burkina Faso|27|BF"},
                {"BI", "Burundi|28|BI"},
                {"KH", "Cambodia|29|KH"},
                {"CM", "Cameroon|30|CM"},
                {"CA", "Canada|31|CA"},
                {"CV", "Cape Verde|33|CV"},
                {"KY", "Cayman Islands|34|KY"},
                {"CF", "Central African Republic|35|CF"},
                {"CL", "Chile|36|CL"},
                {"CN", "China|37|CN"},
                {"CO", "Colombia|38|CO"},
                {"CK", "Cook Islands|39|CK"},
                {"CR", "Costa Rica|40|CR"},
                {"HR", "Croatia|41|HR"},
                {"CU", "Cuba|42|CU"},
                {"CY", "Cyprus|44|CY"},
                {"CZ", "Czech Republic|45|CZ"},
                {"CD", "Democratic Republic of the Congo|46|CD"},
                {"DK", "Denmark|47|DK"},
                {"DJ", "Djibouti|48|DJ"},
                {"DM", "Dominica|49|DM"},
                {"DO", "Dominican Republic|50|DO"},
                {"EC", "Ecuador|51|EC"},
                {"EG", "Egypt|52|EG"},
                {"SV", "El Salvador|53|SV"},
                {"GQ", "Equatorial Guinea|54|GQ"},
                {"ER", "Eritrea|55|ER"},
                {"EE", "Estonia|56|EE"},
                {"ET", "Ethiopia|57|ET"},
                {"FO", "Faroe Islands|58|FO"},
                {"FJ", "Fiji|59|FJ"},
                {"FI", "Finland|60|FI"},
                {"FR", "France|61|FR"},
                {"GF", "French Guiana|62|GF"},
                {"PF", "French Polynesia|63|PF"},
                {"GA", "Gabon|64|GA"},
                {"GM", "Gambia|65|GM"},
                {"GE", "Georgia|66|GE"},
                {"DE", "Germany|67|DE"},
                {"GH", "Ghana|68|GH"},
                {"GI", "Gibraltar|69|GI"},
                {"GR", "Greece|70|GR"},
                {"GL", "Greenland|71|GL"},
                {"GT", "Guatemala|72|GT"},
                {"GN", "Guinea|73|GN"},
                {"GY", "Guyana|74|GY"},
                {"HT", "Haiti|75|HT"},
                {"HN", "Honduras|76|HN"},
                {"HK", "Hong Kong|77|HK"},
                {"HU", "Hungary|78|HU"},
                {"IS", "Iceland|79|IS"},
                {"IN", "India|80|IN"},
                {"ID", "Indonesia|81|ID"},
                {"IR", "Iran|82|IR"},
                {"IQ", "Iraq|83|IQ"},
                {"IE", "Ireland|84|IE"},
                {"IL", "Israel|86|IL"},
                {"IT", "Italy|87|IT"},
                {"JM", "Jamaica|89|JM"},
                {"JP", "Japan|90|JP"},
                {"JO", "Jordan|91|JO"},
                {"KZ", "Kazakhstan|92|KZ"},
                {"KE", "Kenya|93|KE"},
                {"XK", "Kosovo|94|XK"},
                {"KW", "Kuwait|95|KW"},
                {"LA", "Laos|96|LA"},
                {"LV", "Latvia|97|LV"},
                {"LB", "Lebanon|98|LB"},
                {"LS", "Lesotho|99|LS"},
                {"LR", "Liberia|100|LR"},
                {"LY", "Libya|101|LY"},
                {"LT", "Lithuania|102|LT"},
                {"LU", "Luxembourg|103|LU"},
                {"MK", "Macedonia|104|MK"},
                {"MG", "Madagascar|105|MG"},
                {"MW", "Malawi|106|MW"},
                {"MY", "Malaysia|107|MY"},
                {"MV", "Maldives|108|MV"},
                {"ML", "Mali|109|ML"},
                {"MT", "Malta|110|MT"},
                {"MR", "Mauritania|111|MR"},
                {"MU", "Mauritius|112|MU"},
                {"MX", "Mexico|113|MX"},
                {"MD", "Moldova|114|MD"},
                {"MN", "Mongolia|115|MN"},
                {"ME", "Montenegro|116|ME"},
                {"MA", "Morocco|117|MA"},
                {"MZ", "Mozambique|118|MZ"},
                {"MM", "Myanmar|119|MM"},
                {"NA", "Namibia|120|NA"},
                {"NP", "Nepal|121|NP"},
                {"NL", "Netherlands|122|NL"},
                {"NC", "New Caledonia|123|NC"},
                {"NZ", "New Zealand|124|NZ"},
                {"NI", "Nicaragua|125|NI"},
                {"NE", "Niger|126|NE"},
                {"NG", "Nigeria|127|NG"},
                {"NO", "Norway|128|NO"},
                {"OM", "Oman|129|OM"},
                {"PK", "Pakistan|130|PK"},
                {"PS", "Palestinian Territory|131|PS"},
                {"PA", "Panama|132|PA"},
                {"PG", "Papua New Guinea|133|PG"},
                {"PY", "Paraguay|134|PY"},
                {"PE", "Peru|135|PE"},
                {"PH", "Philippines|136|PH"},
                {"PL", "Poland|137|PL"},
                {"PT", "Portugal|138|PT"},
                {"PR", "Puerto Rico|139|PR"},
                {"QA", "Qatar|140|QA"},
                {"CG", "Republic of the Congo|141|CG"},
                {"RE", "Reunion|142|RE"},
                {"RO", "Romania|143|RO"},
                {"RU", "Russia|144|RU"},
                {"RW", "Rwanda|145|RW"},
                {"LC", "Saint Lucia|146|LC"},
                {"SM", "San Marino|147|SM"},
                {"SA", "Saudi Arabia|148|SA"},
                {"SN", "Senegal|149|SN"},
                {"RS", "Serbia|150|RS"},
                {"SC", "Seychelles|151|SC"},
                {"SL", "Sierra Leone|152|SL"},
                {"SG", "Singapore|153|SG"},
                {"SX", "Sint Maarten|154|SX"},
                {"SK", "Slovakia|155|SK"},
                {"SI", "Slovenia|156|SI"},
                {"SO", "Somali|157|SO"},
                {"ZA", "South Africa|159|ZA"},
                {"KR", "South Korea|160|KR"},
                {"SS", "South Sudan|161|SS"},
                {"ES", "Spain|162|ES"},
                {"LK", "Sri Lanka|163|LK"},
                {"SD", "Sudan|165|SD"},
                {"SR", "Suriname|166|SR"},
                {"SZ", "Swaziland|167|SZ"},
                {"SE", "Sweden|168|SE"},
                {"CH", "Switzerland|169|CH"},
                {"SY", "Syria|170|SY"},
                {"TW", "Taiwan|171|TW"},
                {"TZ", "Tanzania|172|TZ"},
                {"TH", "Thailand|173|TH"},
                {"TG", "Togo|174|TG"},
                {"TT", "Trinidad and Tobago|175|TT"},
                {"TN", "Tunisia|176|TN"},
                {"TR", "Turkey|177|TR"},
                {"TM", "Turkmenistan|178|TM"},
                {"UG", "Uganda|179|UG"},
                {"UA", "Ukraine|180|UA"},
                {"AE", "United Arab Emirates|181|AE"},
                {"GB", "United Kingdom|182|GB"},
                {"US", "United States|183|US"},
                {"UY", "Uruguay|184|UY"},
                {"UZ", "Uzbekistan|185|UZ"},
                {"VU", "Vanuatu|186|VU"},
                {"VE", "Venezuela|187|VE"},
                {"VN", "Vietnam|188|VN"},
                {"YE", "Yemen|189|YE"},
                {"ZM", "Zambia|190|ZM"},
                {"ZW", "Zimbabwe|191|ZW"}
        };

        private List<Company> GetCompaniesByCountry(string countryCode, string source)
        {
            // Si la fuente es "GLA", convertir el código del país a su número correspondiente
            if (source == "GLA" && countryIdMap.ContainsKey(countryCode))
            {
                countryCode = countryIdMap[countryCode];
            }
            else if (source == "JCTRANS" && countryIdMap2.ContainsKey(countryCode))
            {
                countryCode = countryIdMap2[countryCode];
            }
            else if (source == "DF" && countryIdMap3.ContainsKey(countryCode))
            {
                countryCode = countryIdMap3[countryCode];
            }

            List<Company> companies = new List<Company>();

            string connectionString = _configuration.GetConnectionString("WebScrapingContext");
            string query = "SELECT \"Name\", \"Href\", \"Address\", \"Phone\", \"Website\", \"Source\", \"Pais\", \"NameContact\", \"Email\", \"Titlecontact\", \"Emailcontact\", \"Mobile\" " +
                           "FROM \"Company\" WHERE \"Pais\" = @Pais AND \"Source\" = @Source";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                using (var command = new NpgsqlCommand(query, connection))
                {
                    // Asignar los parámetros para la consulta SQL
                    command.Parameters.AddWithValue("@Pais", countryCode);
                    command.Parameters.AddWithValue("@Source", source);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        // Leer los resultados y mapearlos a objetos Company
                        while (reader.Read())
                        {
                            companies.Add(new Company
                            {
                                Name = reader["Name"].ToString(),
                                Href = reader["Href"].ToString(),
                                Address = reader["Address"].ToString(),
                                Phone = reader["Phone"].ToString(),
                                Website = reader["Website"].ToString(),
                                Source = reader["Source"].ToString(),
                                Pais = reader["Pais"].ToString(),
                                NameContact = reader["NameContact"].ToString(),
                                Email = reader["Email"].ToString(),
                                Titlecontact = reader["Titlecontact"].ToString(),
                                Emailcontact = reader["Emailcontact"].ToString(),
                                Mobile = reader["Mobile"].ToString()
                            });
                        }
                    }
                }
            }

            return companies;
        }

        // Método para iniciar el proceso de scraping
        public async Task<IActionResult> OnPostAsync()
        {
            LoadCountryList();

            if (string.IsNullOrEmpty(SelectedCountry))
            {
                ModelState.AddModelError(string.Empty, "Por favor, selecciona un país.");
                return Page();
            }

            // Convertir `SelectedCountry` a su ID numérico si es `GLA`
            if (Source == "GLA" && countryIdMap.ContainsKey(SelectedCountry))
            {
                SelectedCountry = countryIdMap[SelectedCountry];
            }
            else if (Source == "JCTRANS" && countryIdMap2.ContainsKey(SelectedCountry))
            {
                SelectedCountry = countryIdMap2[SelectedCountry];
            }
            else if (Source == "DF")
            {
                if (countryIdMap3.ContainsKey(SelectedCountry))
                {
                    SelectedCountry = countryIdMap3[SelectedCountry];
                }
                else
                {
                    // Si el país no está en el mapa de DF, mostramos el mensaje de error
                    Message = $"No se encontraron registros para el país.";
                    return Page();
                }
            }
            

            // Obtener el nombre completo del país seleccionado
            if (Source == "GLA")
            {
                // Buscar la clave en el diccionario para obtener el código alfabético
                var alphaCode = countryIdMap.FirstOrDefault(x => x.Value == SelectedCountry).Key;
                SelectedCountryName = CountryList.FirstOrDefault(c => c.Value == alphaCode)?.Text;
            }
            else if (Source == "JCTRANS")
            {
                var alphaCode = countryIdMap2.FirstOrDefault(x => x.Value == SelectedCountry).Key;
                SelectedCountryName = CountryList.FirstOrDefault(c => c.Value == alphaCode)?.Text;
            }
            else if (Source == "DF")
            {
                var alphaCode = countryIdMap3.FirstOrDefault(x => x.Value == SelectedCountry).Key;
                SelectedCountryName = CountryList.FirstOrDefault(c => c.Value == alphaCode)?.Text;
            }
            else
            {
                SelectedCountryName = CountryList.FirstOrDefault(c => c.Value == SelectedCountry)?.Text;
            }
            // Iniciar el proceso de scraping con manejo de excepciones
            Scraper scraper = new Scraper(_configuration, Source);
            try
            {
                await scraper.StartScrapingAsync(SelectedCountry);

                // Verificar si existen registros para el país seleccionado
                HasRecords = CheckIfRecordsExist(SelectedCountry, Source);

                if (HasRecords)
                {
                    // Cargar la lista de compañías para mostrar en la tabla
                    CompanyList = GetCompaniesByCountry(SelectedCountry, Source);
                    LastScrapingDate = GetLastScrapingDate(SelectedCountry, Source);
                    RecordCount = GetRecordCountByCountry(SelectedCountry, Source);
                    DailyQuotaRemaining = GetDailyQuotaRemaining(SelectedCountry, Source);

                    Message = $"Obtención de datos completado exitosamente para el país: {SelectedCountryName}. Registros actualizados. Puedes exportarlos a Excel.";
                }
                else
                {
                    Message = $"No se encontraron registros para el país: {SelectedCountryName}.";
                }
            }
            catch (MaximumAccessReachedException ex)
            {
                Message = ex.Message;
                HasRecords = false;
            }
            catch (Exception ex)
            {
                Message = "Ocurrió un error durante la obtención de datos.";
                CompanyList = GetCompaniesByCountry(SelectedCountry, Source);
                LastScrapingDate = GetLastScrapingDate(SelectedCountry, Source);
                RecordCount = GetRecordCountByCountry(SelectedCountry, Source);
                DailyQuotaRemaining = GetDailyQuotaRemaining(SelectedCountry, Source);
                HasRecords = true; // Mostrar los registros existentes
            }

            return Page();
        }
        // Método para exportar datos a Excel
        public IActionResult OnPostExportData()
        {
            LoadCountryList();

            if (string.IsNullOrEmpty(SelectedCountry))
            {
                ModelState.AddModelError(string.Empty, "Por favor, selecciona un país.");
                return Page();
            }

            // Convertir SelectedCountry a su ID numérico si Source es "GLA"
            if (Source == "GLA" && countryIdMap.ContainsKey(SelectedCountry))
            {
                SelectedCountry = countryIdMap[SelectedCountry];
            }
            else if (Source == "JCTRANS" && countryIdMap2.ContainsKey(SelectedCountry))
            {
                SelectedCountry = countryIdMap2[SelectedCountry];
            }
            else if (Source == "DF" && countryIdMap3.ContainsKey(SelectedCountry))
            {
                SelectedCountry = countryIdMap3[SelectedCountry];
            }

            // Verificar si existen registros para el país seleccionado
            HasRecords = CheckIfRecordsExist(SelectedCountry, Source);

            if (!HasRecords)
            {
                Message = $"No hay registros para el país: {SelectedCountry}.";
                return Page();
            }

            // Exportar los registros a un archivo Excel
            var fileContent = ExportDataToExcel(SelectedCountry, Source);
            var fileName = $"{SelectedCountry}_Companies.xlsx";
            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }


        // Verificar si existen registros en la base de datos
        private bool CheckIfRecordsExist(string countryCode, string source)
        {
            string connectionString = _configuration.GetConnectionString("WebScrapingContext");
            string query = "SELECT COUNT(*) FROM \"Company\" WHERE \"Pais\" = @Pais AND \"Source\" = @Source";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pais", countryCode);
                    command.Parameters.AddWithValue("@Source", source); // Agrega este parámetro aquí
                    connection.Open();
                    long count = (long)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        public IActionResult OnPostExportAllData()
        {
            if (string.IsNullOrEmpty(Source))
            {
                ModelState.AddModelError(string.Empty, "Por favor, selecciona un network.");
                return Page();
            }
            var fileContent = ExportAllDataToExce2l(Source);
            var fileName = $"All_Companies_{Source}.xls";
            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        private byte[] ExportAllDataToExce2l(string source)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            string connectionString = _configuration.GetConnectionString("WebScrapingContext");
            string query = "SELECT \"Name\", \"Href\", \"Address\", \"Phone\", \"Website\", \"Source\", \"Pais\", \"NameContact\", \"Email\", \"Titlecontact\", \"Emailcontact\", \"Mobile\", \"Fecha\" " +
                           "FROM \"Company\" WHERE \"Source\" = @Source";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                using (var command = new NpgsqlCommand(query, connection))
                {
                    // Asignar el parámetro "Source"
                    command.Parameters.AddWithValue("@Source", source);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        using (var package = new ExcelPackage())
                        {
                            var worksheet = package.Workbook.Worksheets.Add("Companies");

                            // Escribir encabezados
                            worksheet.Cells[1, 1].Value = "Name";
                            worksheet.Cells[1, 2].Value = "Href";
                            worksheet.Cells[1, 3].Value = "Address";
                            worksheet.Cells[1, 4].Value = "Phone";
                            worksheet.Cells[1, 5].Value = "Website";
                            worksheet.Cells[1, 6].Value = "Source";
                            worksheet.Cells[1, 7].Value = "Pais";
                            worksheet.Cells[1, 8].Value = "NameContact";
                            worksheet.Cells[1, 9].Value = "Email";
                            worksheet.Cells[1, 10].Value = "Titlecontact";
                            worksheet.Cells[1, 11].Value = "Emailcontact";
                            worksheet.Cells[1, 12].Value = "Mobile";

                            int row = 2;
                            while (reader.Read())
                            {

                                string name = reader["Name"].ToString();
                                string href = reader["Href"].ToString();
                                string address = reader["Address"].ToString();
                                string phone = reader["Phone"].ToString();
                                string website = reader["Website"].ToString();
                                string source2 = reader["Source"].ToString();
                                string pais = reader["Pais"].ToString();
                                string email = reader["Email"].ToString();


                                string titleContactRaw = reader["Titlecontact"].ToString();
                                titleContactRaw = System.Text.RegularExpressions.Regex.Replace(titleContactRaw, @"\s*\(.*?\)", "").Trim();
                                string[] titleContacts = titleContactRaw.Split(',');


                                string[] nameContacts = reader["NameContact"].ToString().Split(',');
                                string[] emailContacts = reader["Emailcontact"].ToString().Split(',');
                                string[] mobiles = reader["Mobile"].ToString().Split(',');


                                int maxEntries = Math.Max(nameContacts.Length, Math.Max(titleContacts.Length, Math.Max(emailContacts.Length, mobiles.Length)));


                                for (int i = 0; i < maxEntries; i++)
                                {
                                    if (i == 0)
                                    {

                                        worksheet.Cells[row, 1].Value = name;
                                        worksheet.Cells[row, 2].Value = href;
                                        worksheet.Cells[row, 3].Value = address;
                                        worksheet.Cells[row, 4].Value = phone;
                                        worksheet.Cells[row, 5].Value = website;
                                        worksheet.Cells[row, 6].Value = source2;
                                        worksheet.Cells[row, 7].Value = pais;
                                        worksheet.Cells[row, 9].Value = email;
                                    }


                                    worksheet.Cells[row, 8].Value = i < nameContacts.Length ? nameContacts[i].Trim() : "";
                                    worksheet.Cells[row, 10].Value = i < titleContacts.Length ? titleContacts[i].Trim() : "";
                                    worksheet.Cells[row, 11].Value = i < emailContacts.Length ? emailContacts[i].Trim() : "";
                                    worksheet.Cells[row, 12].Value = i < mobiles.Length ? mobiles[i].Trim() : "";

                                    row++;
                                }
                            }

                            return package.GetAsByteArray();
                        }
                    }
                }
            }
        }

        // Exportar los datos a un archivo Excel
        private byte[] ExportDataToExcel(string countryCode, string source)
        {
            // Convertir el código del país a su ID numérico si la fuente es "GLA"
            if (source == "GLA" && countryIdMap.ContainsKey(countryCode))
            {
                countryCode = countryIdMap[countryCode];
            }
            else if (source == "JCTRANS" && countryIdMap2.ContainsKey(countryCode))
            {
                countryCode = countryIdMap2[countryCode];
            }
            else if (source == "DF" && countryIdMap3.ContainsKey(countryCode))
            {
                countryCode = countryIdMap3[countryCode];
            }

            // Establecer el contexto de licencia al inicio del método
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            string connectionString = _configuration.GetConnectionString("WebScrapingContext");
            string query = "SELECT \"Name\", \"Href\", \"Address\", \"Phone\", \"Website\", \"Source\", \"Pais\", \"NameContact\", \"Email\", \"Titlecontact\", \"Emailcontact\", \"Mobile\" FROM \"Company\" WHERE \"Pais\" = @Pais AND \"Source\" = @Source";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pais", countryCode);
                    command.Parameters.AddWithValue("@Source", source);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        using (var package = new ExcelPackage())
                        {
                            var worksheet = package.Workbook.Worksheets.Add("Companies");

                            // Escribir encabezados
                            worksheet.Cells[1, 1].Value = "Name";
                            worksheet.Cells[1, 2].Value = "Href";
                            worksheet.Cells[1, 3].Value = "Address";
                            worksheet.Cells[1, 4].Value = "Phone";
                            worksheet.Cells[1, 5].Value = "Website";
                            worksheet.Cells[1, 6].Value = "Source";
                            worksheet.Cells[1, 7].Value = "Pais";
                            worksheet.Cells[1, 8].Value = "NameContact";
                            worksheet.Cells[1, 9].Value = "Email";
                            worksheet.Cells[1, 10].Value = "Titlecontact";
                            worksheet.Cells[1, 11].Value = "Emailcontact";
                            worksheet.Cells[1, 12].Value = "Mobile";

                            int row = 2;
                            while (reader.Read())
                            {
                                // Valores comunes
                                string name = reader["Name"].ToString();
                                string href = reader["Href"].ToString();
                                string address = reader["Address"].ToString();
                                string phone = reader["Phone"].ToString();
                                string website = reader["Website"].ToString();
                                string source2 = reader["Source"].ToString();
                                string pais = reader["Pais"].ToString();
                                string email = reader["Email"].ToString();

                                // Limpiar y dividir Titlecontact
                                string titleContactRaw = reader["Titlecontact"].ToString();
                                titleContactRaw = System.Text.RegularExpressions.Regex.Replace(titleContactRaw, @"\s*\(.*?\)", "").Trim(); // Eliminar texto entre paréntesis
                                string[] titleContacts = titleContactRaw.Split(','); // Dividir por comas

                                // Dividir los valores de NameContact, Emailcontact y Mobile
                                string[] nameContacts = reader["NameContact"].ToString().Split(',');
                                string[] emailContacts = reader["Emailcontact"].ToString().Split(',');
                                string[] mobiles = reader["Mobile"].ToString().Split(',');

                                // Determinar el número máximo de entradas en los campos divididos
                                int maxEntries = Math.Max(nameContacts.Length, Math.Max(titleContacts.Length, Math.Max(emailContacts.Length, mobiles.Length)));

                                // Crear una fila por cada entrada sin duplicar los valores comunes
                                for (int i = 0; i < maxEntries; i++)
                                {
                                    if (i == 0)
                                    {
                                        // Escribir los valores comunes solo en la primera fila
                                        worksheet.Cells[row, 1].Value = name;
                                        worksheet.Cells[row, 2].Value = href;
                                        worksheet.Cells[row, 3].Value = address;
                                        worksheet.Cells[row, 4].Value = phone;
                                        worksheet.Cells[row, 5].Value = website;
                                        worksheet.Cells[row, 6].Value = source2;
                                        worksheet.Cells[row, 7].Value = pais;
                                        worksheet.Cells[row, 9].Value = email;
                                    }

                                    // Asignar valores adicionales de NameContact, Titlecontact, Emailcontact y Mobile si existen
                                    worksheet.Cells[row, 8].Value = i < nameContacts.Length ? nameContacts[i].Trim() : "";
                                    worksheet.Cells[row, 10].Value = i < titleContacts.Length ? titleContacts[i].Trim() : ""; // Titlecontact ya dividido
                                    worksheet.Cells[row, 11].Value = i < emailContacts.Length ? emailContacts[i].Trim() : "";
                                    worksheet.Cells[row, 12].Value = i < mobiles.Length ? mobiles[i].Trim() : "";

                                    row++;
                                }
                            }

                            return package.GetAsByteArray();
                        }
                    }
                }
            }
        }


        // Método para cerrar sesión
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToPage("/Login");
        }
        private DateTime? GetLastScrapingDate(string countryCode, string source)
        {
            string connectionString = _configuration.GetConnectionString("WebScrapingContext");
            string query = "SELECT MAX(\"Fecha\") FROM \"Company\" WHERE \"Pais\" = @Pais  AND \"Source\" = @Source";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pais", countryCode);
                    command.Parameters.AddWithValue("@Source", source);
                    connection.Open();

                    var result = command.ExecuteScalar();
                    if (result != DBNull.Value)
                    {
                        return (DateTime?)result;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
        private int GetRecordCountByCountry(string countryCode, string source)
        {
            string connectionString = _configuration.GetConnectionString("WebScrapingContext");
            string query = "SELECT COUNT(*) FROM \"Company\" WHERE \"Pais\" = @Pais AND \"Source\" = @Source";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pais", countryCode);
                    command.Parameters.AddWithValue("@Source", source);
                    connection.Open();

                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }
        private int GetDailyQuotaRemaining(string countryCode, string source)
        {
            string connectionString = _configuration.GetConnectionString("WebScrapingContext");
            string query = "SELECT COUNT(*) FROM \"Company\" WHERE DATE(\"Fecha\") = CURRENT_DATE AND \"Source\" = @Source";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pais", countryCode);
                    command.Parameters.AddWithValue("@Source", source);
                    connection.Open();

                    int todayCount = Convert.ToInt32(command.ExecuteScalar());
                    return Math.Max(600 - todayCount, 0); // Resta de 600 y asegura que no baje de 0
                }
            }
        }

        private void LoadCountryList()
        {
            // Mantiene la lista de países
            CountryList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "--- Country ---" },
                new SelectListItem { Value = "AF", Text = "Afghanistan" },
                new SelectListItem { Value = "AX", Text = "Aland Islands" },
                new SelectListItem { Value = "AL", Text = "Albania" },
                new SelectListItem { Value = "DZ", Text = "Algeria" },
                new SelectListItem { Value = "AS", Text = "American Samoa" },
                new SelectListItem { Value = "AD", Text = "Andorra" },
                new SelectListItem { Value = "AO", Text = "Angola" },
                new SelectListItem { Value = "AI", Text = "Anguilla" },
                new SelectListItem { Value = "AQ", Text = "Antarctica" },
                new SelectListItem { Value = "AG", Text = "Antigua And Barbuda" },
                new SelectListItem { Value = "AR", Text = "Argentina" },
                new SelectListItem { Value = "AM", Text = "Armenia" },
                new SelectListItem { Value = "AW", Text = "Aruba" },
                new SelectListItem { Value = "AU", Text = "Australia" },
                new SelectListItem { Value = "AT", Text = "Austria" },
                new SelectListItem { Value = "AZ", Text = "Azerbaijan" },
                new SelectListItem { Value = "BS", Text = "Bahamas" },
                new SelectListItem { Value = "BH", Text = "Bahrain" },
                new SelectListItem { Value = "BD", Text = "Bangladesh" },
                new SelectListItem { Value = "BB", Text = "Barbados" },
                new SelectListItem { Value = "BY", Text = "Belarus" },
                new SelectListItem { Value = "BE", Text = "Belgium" },
                new SelectListItem { Value = "BZ", Text = "Belize" },
                new SelectListItem { Value = "BJ", Text = "Benin" },
                new SelectListItem { Value = "BM", Text = "Bermuda" },
                new SelectListItem { Value = "BT", Text = "Bhutan" },
                new SelectListItem { Value = "BO", Text = "Bolivia" },
                new SelectListItem { Value = "BQ", Text = "Bonaire" },
                new SelectListItem { Value = "BA", Text = "Bosnia and Herzegovina" },
                new SelectListItem { Value = "BW", Text = "Botswana" },
                new SelectListItem { Value = "BV", Text = "Bouvet Island" },
                new SelectListItem { Value = "BR", Text = "Brazil" },
                new SelectListItem { Value = "IO", Text = "British Indian Ocean Territory" },
                new SelectListItem { Value = "BN", Text = "Brunei Darussalam" },
                new SelectListItem { Value = "BG", Text = "Bulgaria" },
                new SelectListItem { Value = "BF", Text = "Burkina Faso" },
                new SelectListItem { Value = "BI", Text = "Burundi" },
                new SelectListItem { Value = "BU", Text = "Byelorussia" },
                new SelectListItem { Value = "KH", Text = "Cambodia" },
                new SelectListItem { Value = "CM", Text = "Cameroon" },
                new SelectListItem { Value = "CA", Text = "Canada" },
                new SelectListItem { Value = "CV", Text = "Cape Verde" },
                new SelectListItem { Value = "KY", Text = "Cayman Islands" },
                new SelectListItem { Value = "CF", Text = "Central African Republic" },
                new SelectListItem { Value = "TD", Text = "Chad" },
                new SelectListItem { Value = "CL", Text = "Chile" },
                new SelectListItem { Value = "CN", Text = "China" },
                new SelectListItem { Value = "CX", Text = "Christmas Island" },
                new SelectListItem { Value = "CC", Text = "Cocos (Keeling) Islands" },
                new SelectListItem { Value = "CO", Text = "Colombia" },
                new SelectListItem { Value = "KM", Text = "Comoros" },
                new SelectListItem { Value = "CG", Text = "Congo" },
                new SelectListItem { Value = "CD", Text = "Congo, The Democratic Republic Of The" },
                new SelectListItem { Value = "CK", Text = "Cook Islands" },
                new SelectListItem { Value = "CR", Text = "Costa Rica" },
                new SelectListItem { Value = "CI", Text = "Cote D'Ivoire" },
                new SelectListItem { Value = "HR", Text = "Croatia" },
                new SelectListItem { Value = "CU", Text = "Cuba" },
                new SelectListItem { Value = "CW", Text = "Curacao" },
                new SelectListItem { Value = "CY", Text = "Cyprus" },
                new SelectListItem { Value = "CZ", Text = "Czech Republic" },
                new SelectListItem { Value = "DK", Text = "Denmark" },
                new SelectListItem { Value = "DJ", Text = "Djibouti" },
                new SelectListItem { Value = "DM", Text = "Dominica" },
                new SelectListItem { Value = "DO", Text = "Dominican Republic" },
                new SelectListItem { Value = "EC", Text = "Ecuador" },
                new SelectListItem { Value = "EG", Text = "Egypt" },
                new SelectListItem { Value = "SV", Text = "El Salvador" },
                new SelectListItem { Value = "GQ", Text = "Equatorial Guinea" },
                new SelectListItem { Value = "ER", Text = "Eritrea" },
                new SelectListItem { Value = "EE", Text = "Estonia" },
                new SelectListItem { Value = "ET", Text = "Ethiopia" },
                new SelectListItem { Value = "FK", Text = "Falkland Islands (Malvinas)" },
                new SelectListItem { Value = "FO", Text = "Faroe Islands" },
                new SelectListItem { Value = "FJ", Text = "Fiji" },
                new SelectListItem { Value = "FI", Text = "Finland" },
                new SelectListItem { Value = "FR", Text = "France" },
                new SelectListItem { Value = "GF", Text = "French Guiana" },
                new SelectListItem { Value = "PF", Text = "French Polynesia" },
                new SelectListItem { Value = "TF", Text = "French Southern Territories" },
                new SelectListItem { Value = "GA", Text = "Gabon" },
                new SelectListItem { Value = "GM", Text = "Gambia" },
                new SelectListItem { Value = "GE", Text = "Georgia" },
                new SelectListItem { Value = "DE", Text = "Germany" },
                new SelectListItem { Value = "GH", Text = "Ghana" },
                new SelectListItem { Value = "GI", Text = "Gibraltar" },
                new SelectListItem { Value = "GR", Text = "Greece" },
                new SelectListItem { Value = "GL", Text = "Greenland" },
                new SelectListItem { Value = "GD", Text = "Grenada" },
                new SelectListItem { Value = "GP", Text = "Guadeloupe" },
                new SelectListItem { Value = "GU", Text = "Guam" },
                new SelectListItem { Value = "GT", Text = "Guatemala" },
                new SelectListItem { Value = "GN", Text = "Guinea" },
                new SelectListItem { Value = "GW", Text = "Guinea-bissau" },
                new SelectListItem { Value = "GY", Text = "Guyana" },
                new SelectListItem { Value = "HT", Text = "Haiti" },
                new SelectListItem { Value = "HM", Text = "Heard Island And Mcdonald Islands" },
                new SelectListItem { Value = "VA", Text = "Holy See (Vatican City State)" },
                new SelectListItem { Value = "HN", Text = "Honduras" },
                new SelectListItem { Value = "HK", Text = "Hong Kong, China" },
                new SelectListItem { Value = "HU", Text = "Hungary" },
                new SelectListItem { Value = "IS", Text = "Iceland" },
                new SelectListItem { Value = "IN", Text = "India" },
                new SelectListItem { Value = "ID", Text = "Indonesia" },
                new SelectListItem { Value = "IR", Text = "Iran, Islamic Republic Of" },
                new SelectListItem { Value = "IQ", Text = "Iraq" },
                new SelectListItem { Value = "IE", Text = "Ireland" },
                new SelectListItem { Value = "IL", Text = "Israel" },
                new SelectListItem { Value = "IT", Text = "Italy" },
                new SelectListItem { Value = "JM", Text = "Jamaica" },
                new SelectListItem { Value = "JP", Text = "Japan" },
                new SelectListItem { Value = "JO", Text = "Jordan" },
                new SelectListItem { Value = "KZ", Text = "Kazakhstan" },
                new SelectListItem { Value = "KE", Text = "Kenya" },
                new SelectListItem { Value = "KI", Text = "Kiribati" },
                new SelectListItem { Value = "KP", Text = "Korea, Democratic People`s Republic Of" },
                new SelectListItem { Value = "KR", Text = "Korea, Republic Of" },
                new SelectListItem { Value = "XK", Text = "Kosovo" },
                new SelectListItem { Value = "KW", Text = "Kuwait" },
                new SelectListItem { Value = "KG", Text = "Kyrgyzstan" },
                new SelectListItem { Value = "LA", Text = "Lao People`s Democratic Republic" },
                new SelectListItem { Value = "LV", Text = "Latvia" },
                new SelectListItem { Value = "LB", Text = "Lebanon" },
                new SelectListItem { Value = "LS", Text = "Lesotho" },
                new SelectListItem { Value = "LR", Text = "Liberia" },
                new SelectListItem { Value = "LY", Text = "Libya" },
                new SelectListItem { Value = "LI", Text = "Liechtenstein" },
                new SelectListItem { Value = "LT", Text = "Lithuania" },
                new SelectListItem { Value = "LU", Text = "Luxembourg" },
                new SelectListItem { Value = "MO", Text = "Macau, China" },
                new SelectListItem { Value = "MK", Text = "Macedonia" },
                new SelectListItem { Value = "MG", Text = "Madagascar" },
                new SelectListItem { Value = "MW", Text = "Malawi" },
                new SelectListItem { Value = "MY", Text = "Malaysia" },
                new SelectListItem { Value = "MV", Text = "Maldives" },
                new SelectListItem { Value = "ML", Text = "Mali" },
                new SelectListItem { Value = "MT", Text = "Malta" },
                new SelectListItem { Value = "MH", Text = "Marshall Islands" },
                new SelectListItem { Value = "MQ", Text = "Martinique" },
                new SelectListItem { Value = "MR", Text = "Mauritania" },
                new SelectListItem { Value = "MU", Text = "Mauritius" },
                new SelectListItem { Value = "YT", Text = "Mayotte" },
                new SelectListItem { Value = "MX", Text = "Mexico" },
                new SelectListItem { Value = "FM", Text = "Micronesia, Federated States Of" },
                new SelectListItem { Value = "MD", Text = "Moldova, Republic Of" },
                new SelectListItem { Value = "MC", Text = "Monaco" },
                new SelectListItem { Value = "MN", Text = "Mongolia" },
                new SelectListItem { Value = "ME", Text = "Montenegro" },
                new SelectListItem { Value = "MS", Text = "Montserrat" },
                new SelectListItem { Value = "MA", Text = "Morocco" },
                new SelectListItem { Value = "MZ", Text = "Mozambique" },
                new SelectListItem { Value = "MM", Text = "Myanmar" },
                new SelectListItem { Value = "NA", Text = "Namibia" },
                new SelectListItem { Value = "NR", Text = "Nauru" },
                new SelectListItem { Value = "NP", Text = "Nepal" },
                new SelectListItem { Value = "NL", Text = "Netherlands" },
                new SelectListItem { Value = "AN", Text = "Netherlands Antilles" },
                new SelectListItem { Value = "NC", Text = "New Caledonia" },
                new SelectListItem { Value = "NZ", Text = "New Zealand" },
                new SelectListItem { Value = "NI", Text = "Nicaragua" },
                new SelectListItem { Value = "NE", Text = "Niger" },
                new SelectListItem { Value = "NG", Text = "Nigeria" },
                new SelectListItem { Value = "NU", Text = "Niue" },
                new SelectListItem { Value = "NF", Text = "Norfolk Island" },
                new SelectListItem { Value = "NO", Text = "Norway" },
                new SelectListItem { Value = "OM", Text = "Oman" },
                new SelectListItem { Value = "PK", Text = "Pakistan" },
                new SelectListItem { Value = "PW", Text = "Palau" },
                new SelectListItem { Value = "PS", Text = "Palestinian Territory, Occupied" },
                new SelectListItem { Value = "PA", Text = "Panama" },
                new SelectListItem { Value = "PG", Text = "Papua New Guinea" },
                new SelectListItem { Value = "PY", Text = "Paraguay" },
                new SelectListItem { Value = "PE", Text = "Peru" },
                new SelectListItem { Value = "PH", Text = "Philippines" },
                new SelectListItem { Value = "PN", Text = "Pitcairn" },
                new SelectListItem { Value = "PL", Text = "Poland" },
                new SelectListItem { Value = "PT", Text = "Portugal" },
                new SelectListItem { Value = "PR", Text = "Puerto Rico" },
                new SelectListItem { Value = "QA", Text = "Qatar" },
                new SelectListItem { Value = "RE", Text = "Reunion" },
                new SelectListItem { Value = "RO", Text = "Romania" },
                new SelectListItem { Value = "RU", Text = "Russian Federation" },
                new SelectListItem { Value = "RW", Text = "Rwanda" },
                new SelectListItem { Value = "BL", Text = "Saint Barthelmey" },
                new SelectListItem { Value = "SH", Text = "Saint Helena" },
                new SelectListItem { Value = "KN", Text = "Saint Kitts And Nevis" },
                new SelectListItem { Value = "LC", Text = "Saint Lucia" },
                new SelectListItem { Value = "PM", Text = "Saint Pierre And Miquelon" },
                new SelectListItem { Value = "TS", Text = "Saint Thomas" },
                new SelectListItem { Value = "VC", Text = "Saint Vincent And The Grenadines" },
                new SelectListItem { Value = "MP", Text = "Saipan" },
                new SelectListItem { Value = "WS", Text = "Samoa" },
                new SelectListItem { Value = "SM", Text = "San Marino" },
                new SelectListItem { Value = "ST", Text = "Sao Tome And Principe" },
                new SelectListItem { Value = "SA", Text = "Saudi Arabia" },
                new SelectListItem { Value = "SN", Text = "Senegal" },
                new SelectListItem { Value = "RS", Text = "Serbia" },
                new SelectListItem { Value = "SC", Text = "Seychelles" },
                new SelectListItem { Value = "SL", Text = "Sierra Leone" },
                new SelectListItem { Value = "SG", Text = "Singapore" },
                new SelectListItem { Value = "SX", Text = "Sint Maarten" },
                new SelectListItem { Value = "SK", Text = "Slovakia" },
                new SelectListItem { Value = "SI", Text = "Slovenia" },
                new SelectListItem { Value = "SB", Text = "Solomon Islands" },
                new SelectListItem { Value = "SO", Text = "Somalia" },
                new SelectListItem { Value = "ZA", Text = "South Africa" },
                new SelectListItem { Value = "GS", Text = "South Georgia And The South Sandwich Islands" },
                new SelectListItem { Value = "SS", Text = "South Sudan" },
                new SelectListItem { Value = "ES", Text = "Spain" },
                new SelectListItem { Value = "LK", Text = "Sri Lanka" },
                new SelectListItem { Value = "SD", Text = "Sudan" },
                new SelectListItem { Value = "SR", Text = "Suriname" },
                new SelectListItem { Value = "SJ", Text = "Svalbard And Jan Mayen" },
                new SelectListItem { Value = "SZ", Text = "Swaziland" },
                new SelectListItem { Value = "SE", Text = "Sweden" },
                new SelectListItem { Value = "CH", Text = "Switzerland" },
                new SelectListItem { Value = "SY", Text = "Syrian Arab Republic" },
                new SelectListItem { Value = "TW", Text = "Taiwan, China" },
                new SelectListItem { Value = "TJ", Text = "Tajikistan" },
                new SelectListItem { Value = "TZ", Text = "Tanzania, United Republic Of" },
                new SelectListItem { Value = "TH", Text = "Thailand" },
                new SelectListItem { Value = "TL", Text = "Timor-leste" },
                new SelectListItem { Value = "TG", Text = "Togo" },
                new SelectListItem { Value = "TK", Text = "Tokelau" },
                new SelectListItem { Value = "TO", Text = "Tonga" },
                new SelectListItem { Value = "TT", Text = "Trinidad And Tobago" },
                new SelectListItem { Value = "TN", Text = "Tunisia" },
                new SelectListItem { Value = "TR", Text = "Turkiye" },
                new SelectListItem { Value = "TM", Text = "Turkmenistan" },
                new SelectListItem { Value = "TC", Text = "Turks and Caicos Islands" },
                new SelectListItem { Value = "TV", Text = "Tuvalu" },
                new SelectListItem { Value = "UG", Text = "Uganda" },
                new SelectListItem { Value = "UA", Text = "Ukraine" },
                new SelectListItem { Value = "AE", Text = "United Arab Emirates" },
                new SelectListItem { Value = "GB", Text = "United Kingdom" },
                new SelectListItem { Value = "UM", Text = "United States Minor Outlying Islands" },
                new SelectListItem { Value = "US", Text = "United States of America" },
                new SelectListItem { Value = "UY", Text = "Uruguay" },
                new SelectListItem { Value = "UZ", Text = "Uzbekistan" },
                new SelectListItem { Value = "VU", Text = "Vanuatu" },
                new SelectListItem { Value = "VE", Text = "Venezuela" },
                new SelectListItem { Value = "VN", Text = "Vietnam" },
                new SelectListItem { Value = "VG", Text = "Virgin Islands, British" },
                new SelectListItem { Value = "VI", Text = "Virgin Islands, U.S." },
                new SelectListItem { Value = "WF", Text = "Wallis And Futuna" },
                new SelectListItem { Value = "EH", Text = "Western Sahara" },
                new SelectListItem { Value = "YE", Text = "Yemen" },
                new SelectListItem { Value = "ZM", Text = "Zambia" },
                new SelectListItem { Value = "ZW", Text = "Zimbabwe" }
            };
        }
    }
}
