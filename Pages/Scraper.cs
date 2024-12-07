using HtmlAgilityPack;
using Npgsql;
using ScrapySharp.Extensions;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Net.Http.Headers;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.BiDi.Modules.Script;
using System.Security.Policy;

namespace WebScraping
{
    public class MaximumAccessReachedException : Exception
    {
        public MaximumAccessReachedException(string message) : base(message)
        {
        }
    }

    public class Scraper
    {
        private IWebDriver _driver;
        private Dictionary<string, HttpClient> _httpClients = new Dictionary<string, HttpClient>();
        private Dictionary<string, CookieContainer> _cookieContainers = new Dictionary<string, CookieContainer>();
        private readonly IConfiguration _configuration;
        private readonly string _source;


        public Scraper(IConfiguration configuration, string source)
        {
            var chromeOptions = new ChromeOptions();

           
                // Modo headless para otras fuentes
                chromeOptions.AddArgument("--headless");
            

            chromeOptions.AddArgument("--no-sandbox");
            chromeOptions.AddArgument("--disable-dev-shm-usage");
            chromeOptions.AddArgument("--disable-gpu");
            chromeOptions.AddArgument("--disable-blink-features=AutomationControlled");
            chromeOptions.AddArgument("--ignore-certificate-errors");
            chromeOptions.AddArgument("--allow-insecure-localhost");

            // Configuraciones relacionadas con WebGL
            chromeOptions.AddArgument("--enable-unsafe-swiftshader");
            chromeOptions.AddArgument("--disable-web-security");
            chromeOptions.AddArgument("--disable-software-rasterizer");
            chromeOptions.AddArgument("--use-gl=swiftshader");

            // User-Agent para evitar detección como bot
            chromeOptions.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            _driver = new ChromeDriver(chromeOptions);
            _configuration = configuration;
            _source = source;
        }


        public async Task StartScrapingAsync(string countryCode)
        {
            bool loginSuccess = _source switch
            {
                "GLA" => await LoginWithSeleniumForGLAAsync(),
                "JCTRANS" => await LoginWithSeleniumForJCTRANSsync(),
                "DF" => await LoginWithSeleniumForDFAsync(countryCode),
                _ => await LoginWithSeleniumAsync()
            };


            if (loginSuccess)
            {
                Console.WriteLine($"Inicio de sesión exitoso en {_source}.");
                if (_source == "GLA")
                {
                    DeleteExistingRecords(countryCode, _source);
                    await ScrapeAllPagesForGLAAsync(countryCode);
                }else if (_source == "JCTRANS")
                {
                    DeleteExistingRecords(countryCode, _source);
                    await ScrapeAllPagesForJCTRANSAsync(countryCode);
                }
                else
                {
                    int pageIndex = 1;
                    bool hasMoreResults = true;

                    while (hasMoreResults)
                    {
                        hasMoreResults = await ScrapeDataAsync(pageIndex, countryCode, _source);
                        pageIndex++;
                    }
                }
            }
            else
            {
                Console.WriteLine($"Error al iniciar sesión en {_source}.");
                throw new Exception($"Error al iniciar sesión en {_source}.");
            }

            _driver.Quit();
        }
        private async Task<bool> LoginWithSeleniumAsync()
        {
            try
            {
                // Navegar a la página de inicio de sesión
                _driver.Navigate().GoToUrl("https://www.wcaworld.com/Account/Login");

                // Esperar un momento para asegurarse de que la página esté cargada
                await Task.Delay(5000);

                // Rellenar el campo de nombre de usuario
                var usernameField = _driver.FindElement(By.Id("usr"));
                usernameField.SendKeys("sppgstg");

                // Rellenar el campo de contraseña
                var passwordField = _driver.FindElement(By.Id("pwd"));
                passwordField.SendKeys("Sm!G4F6Qc5");

                // Hacer clic en el botón de inicio de sesión
                var loginButton = _driver.FindElement(By.Id("login-form-button"));
                loginButton.Click();

                // Esperar un momento para que la respuesta del servidor se procese
                await Task.Delay(3000);

                // Verificar si el inicio de sesión fue exitoso comprobando la URL actual
                bool loginSuccessful = _driver.Url.Contains("MemberSection");
                Console.WriteLine(loginSuccessful ? "Inicio de sesión exitoso" : "Error al iniciar sesión");

                return loginSuccessful;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrió un error durante el inicio de sesión: {ex.Message}");
                return false;
            }
        }
        private async Task<bool> LoginWithSeleniumForGLAAsync()
        {
            try
            {
                // Navegar a la página de inicio de sesión de GLA
                _driver.Navigate().GoToUrl("https://www.glafamily.com/login.php");
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

                // Rellenar el campo de correo electrónico del usuario
                var usernameField = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.Id("user_emailsl")));
                usernameField.SendKeys("lherrera@sclcargo.cl");

                // Rellenar el campo de contraseña
                var passwordField = _driver.FindElement(By.Name("password"));
                passwordField.SendKeys("glafamily123");

                // Marcar la casilla "Remember Me"
                var rememberMeCheckbox = _driver.FindElement(By.Name("remember"));
                if (!rememberMeCheckbox.Selected)
                {
                    rememberMeCheckbox.Click();
                }

                // Hacer scroll hacia el botón y clic
                var loginButton = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.CssSelector("input.tjbtn[type='submit']")));
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView(true);", loginButton);
                loginButton.Click();

                // Esperar la respuesta del servidor
                await Task.Delay(10000);

                // Verificar si el inicio de sesión fue exitoso
                bool loginSuccessful = _driver.Url.Contains("dashboard") || _driver.PageSource.Contains("Welcome");
                Console.WriteLine(loginSuccessful ? "Inicio de sesión exitoso en GLA" : "Error al iniciar sesión en GLA");

                return loginSuccessful;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrió un error durante el inicio de sesión en GLA: {ex.Message}");
                return false;
            }
        }
        private async Task<bool> LoginWithSeleniumForJCTRANSsync()
        {
            try
            {
                // Navegar a la página de inicio de sesión de JCTrans
                _driver.Navigate().GoToUrl("https://passport.jctrans.com/login?appId=ERA&path=%2Fen%2Finquiry");
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

                // Esperar a que la página cargue completamente
                wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));

                // Borrar cookies y almacenamiento local
                _driver.Manage().Cookies.DeleteAllCookies();
                ((IJavaScriptExecutor)_driver).ExecuteScript("window.localStorage.clear();");
                ((IJavaScriptExecutor)_driver).ExecuteScript("window.sessionStorage.clear();");
                Console.WriteLine("Cookies, caché y almacenamiento local borrados.");

                // Encontrar y rellenar el campo de correo electrónico
                var usernameField = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(
                    By.XPath("//input[@type='text' and contains(@class,'el-input__inner')]")
                ));
                usernameField.SendKeys("lherrera@sclcargo.cl");

                // Encontrar y rellenar el campo de contraseña
                var passwordField = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(
                    By.XPath("//input[@type='password' and contains(@class,'el-input__inner')]")
                ));
                passwordField.SendKeys("Fw6DT.AF.nCndiQ");

                // Hacer clic en el botón de inicio de sesión
                var loginButton = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(
                    By.CssSelector("button.login-button")
                ));
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView(true);", loginButton);
                loginButton.Click();

                // Esperar un momento para que el modal se cargue si aparece
                await Task.Delay(5000);

                // Verificar si aparece el modal
                var modalNode = _driver.FindElements(By.XPath("//div[contains(@class, 'el-message-box')]"));
                if (modalNode.Count > 0)
                {
                    Console.WriteLine("Modal detectado: El inicio de sesión ya está activo.");
                    // Considerar el inicio de sesión como exitoso
                    return true;
                }

                // Esperar la respuesta del servidor
                await Task.Delay(10000);

                // Verificar si el inicio de sesión fue exitoso
                bool loginSuccessful = !_driver.Url.Contains("login");
                Console.WriteLine(loginSuccessful ? "Inicio de sesión exitoso en JCTrans" : "Error al iniciar sesión en JCTrans");

                return loginSuccessful;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrió un error durante el inicio de sesión en JCTrans: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> LoginWithSeleniumForDFAsync(string countryCode)
        {
            try
            {
                string countryName = countryCode.Split('|')[0]; // Obtiene "Albania"
                // Configuración del navegador en modo headless
                var options = new ChromeOptions();
                options.AddArgument("--headless"); // Modo sin interfaz gráfica
                options.AddArgument("--disable-gpu");
                options.AddArgument("--window-size=1920,1080");
                options.AddArgument("--disable-blink-features=AutomationControlled");
                options.AddArgument("--ignore-certificate-errors"); // Ignorar errores SSL
                options.AddArgument("--allow-insecure-localhost");  // Permitir conexiones inseguras locales
                options.AddArgument("--ignore-ssl-errors"); // Ignorar errores SSL adicionales
                options.AddArgument("--no-sandbox"); // Mejora de compatibilidad

                using (var driver = new ChromeDriver(options))
                {
                    driver.Navigate().GoToUrl("https://www.df-alliance.com/auth/sign-in");
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));


                    // Verificar si aparece el modal
                    try
                    {
                        var modalCloseButton = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector("svg.modal__close-icon")));
                        if (modalCloseButton != null)
                        {
                            Console.WriteLine("Modal encontrado. Cerrando...");
                            modalCloseButton.Click();
                            await Task.Delay(10000); // Esperar un poco para que el modal se cierre
                        }
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine("No se encontró el modal. Continuando...");
                    }

                    // Rellenar el campo de usuario
                    var usernameField = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector("input[name='login']")));
                    usernameField.Clear();
                    usernameField.SendKeys("lherrera@sclcargo.cl");

                    // Rellenar el campo de contraseña
                    var passwordField = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector("input[name='password']")));
                    passwordField.Clear();
                    passwordField.SendKeys("Estereot1po.");

                    // Simular comportamiento humano
                    await Task.Delay(new Random().Next(1000, 3000)); // Esperar entre 1 y 3 segundos

                    // Hacer clic en el botón de inicio de sesión
                    var loginButton = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.CssSelector("button.btn.btn-dark.btn-full")));
                    loginButton.Click();

                    // Esperar respuesta
                    await Task.Delay(25000); // Tiempo para que el servidor procese el inicio de sesión

                    // Verificar si el inicio de sesión fue exitoso
                    bool loginSuccessful = !driver.Url.Contains("auth/sign-in") && !driver.PageSource.Contains("invalid");
                    Console.WriteLine(loginSuccessful ? "Inicio de sesión exitoso en DF" : "Error al iniciar sesión en DF");

                    if (!loginSuccessful)
                    {
                        Console.WriteLine("Error al iniciar sesión en DF.");
                        return false;
                    }
                    Console.WriteLine("Inicio de sesión exitoso en DF.");

                    // Navegar al directorio
                    string baseUrl = "https://www.df-alliance.com/directory";
                    _driver.Navigate().GoToUrl(baseUrl);
                    Console.WriteLine($"Navegando a {baseUrl}");
                    // Esperar que la página cargue
                    try
                    {
                        wait.Until(driver => driver.FindElement(By.CssSelector("input.select__panel-text[placeholder='Country']")));
                        Console.WriteLine("Página cargada correctamente.");
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine("La página tardó demasiado en cargar.");
                        return false;
                    }

                    // Seleccionar el país
                    try
                    {
                        // Buscar el input de selección de país
                        var countryInput = driver.FindElement(By.CssSelector("input.select__panel-text[placeholder='Country']"));
                        countryInput.Click();
                        await Task.Delay(10000); // Simular tiempo de interacción

                        // Construir el XPath para el país
                        string xpath = $"//li[contains(@class, 'select__item') and contains(text(), '{countryName}')]";

                        // Buscar la opción del país
                        var countryOptions = driver.FindElements(By.XPath(xpath));

                        // Validar si se encontró el país
                        if (countryOptions.Count == 0)
                        {
                            Console.WriteLine($"El país '{countryName}' no existe en la lista.");
                            return false;
                        }

                        // Seleccionar el país si existe
                        var countryOption = countryOptions.First();
                        countryOption.Click();
                        Console.WriteLine($"País '{countryName}' seleccionado.");
                    }
                    catch (NoSuchElementException)
                    {
                        Console.WriteLine($"No se encontró la opción para el país '{countryName}'.");
                        return false;
                    }

                    // Hacer clic en el botón de búsqueda
                    try
                    {
                        var searchButton = wait.Until(driver => driver.FindElement(By.CssSelector("button.d-filter__btn[type='submit']")));
                        searchButton.Click();
                        Console.WriteLine("Botón de búsqueda clickeado.");
                        await Task.Delay(15000); // Esperar para la carga de resultados
                    }
                    catch (NoSuchElementException)
                    {
                        Console.WriteLine("No se encontró el botón de búsqueda.");
                        return false;
                    }

                    // Verificar si existen resultados en la página
                    bool hasResults = true;
                    try
                    {
                        var noResultsElement = wait.Until(driver => driver.FindElement(By.CssSelector("h3.directory__empty-title")));
                        Console.WriteLine("No se encontraron resultados en la página.");
                        hasResults = false; // Establecer en falso si encontramos el mensaje
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine("No se encontraron resultados en la página.");
                    }
                    if (hasResults)
                    {
                        Console.WriteLine("Eliminando registros existentes para el país seleccionado...");
                        DeleteExistingRecords(countryCode, _source);
                        // Recorrer resultados y manejar paginado
                        while (true)
                        {
                            try
                            {
                                // Esperar a que se cargue la lista
                                var resultsList = wait.Until(driver => driver.FindElement(By.CssSelector("ul.d-table__table-list")));
                                var results = resultsList.FindElements(By.CssSelector("li.d-table__item"));

                                // Procesar resultados
                                foreach (var result in results)
                                {
                                    try
                                    {
                                        var companyName = result.FindElement(By.CssSelector("div[aria-label]")).GetAttribute("aria-label");

                                        var nameContactsStr = result.FindElement(By.CssSelector("div[aria-label] a > b")).Text;

                                        var emailContactsStr = result.FindElement(By.CssSelector("div[data-info] a[href^='mailto:']")).Text;
                                        var mainPhone = result.FindElement(By.CssSelector("div a[href^='tel:']")).Text;
                                        Console.WriteLine($"Empresa: {companyName}, Contacto: {nameContactsStr}, Email: {emailContactsStr}, Teléfono: {mainPhone}");

                                        InsertIntoDatabase(
                                               companyName,
                                               "No url",
                                               "No address",
                                               mainPhone,
                                               "No website",
                                                _source,
                                               countryCode,
                                               nameContactsStr,
                                               emailContactsStr,
                                               "No titleContactsStr",
                                               emailContactsStr,
                                               mainPhone
                                           );

                                    }
                                    catch (NoSuchElementException)
                                    {
                                        Console.WriteLine("Error al procesar un resultado.");
                                    }
                                }

                                // Intentar avanzar a la siguiente página
                                var nextPageButton = driver.FindElements(By.CssSelector("button[aria-label='Go to next page']"))
                                                          .FirstOrDefault(btn => btn.Enabled);

                                if (nextPageButton != null)
                                {
                                    nextPageButton.Click();
                                    Console.WriteLine("Avanzando a la siguiente página...");
                                    await Task.Delay(15000); // Esperar para que cargue la página
                                }
                                else
                                {
                                    Console.WriteLine("No hay más páginas disponibles.");
                                    break;
                                }
                            }
                            catch (WebDriverTimeoutException)
                            {
                                Console.WriteLine("La página tardó demasiado en cargar.");
                                return false;
                            }
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrió un error durante el inicio de sesión en DF: {ex.Message}");
                return false;
            }
        }


        private async Task<bool> ScrapeDataAsync(int pageIndex, string countryCode, string source)
        {
            string url = $"https://www.wcaworld.com/Directory?siteID=24&au=&pageIndex={pageIndex}&pageSize=50&searchby=CountryCode&country={countryCode}&city=&keyword=&orderby=CountryCity&networkIds=1&networkIds=2&networkIds=3&networkIds=4&networkIds=61&networkIds=98&networkIds=108&networkIds=118&networkIds=5&networkIds=22&networkIds=13&networkIds=18&networkIds=15&networkIds=16&networkIds=38&networkIds=103&layout=v1&submitted=search";

            _driver.Navigate().GoToUrl(url);
            await Task.Delay(5000);  // Esperar a que se cargue la página

            // Procesar el contenido de la página
            var pageContent = _driver.PageSource;
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(pageContent);

            // Verificar si el mensaje de error está presente
            var alertNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'alert-danger') and contains(@class, 'text-center') and contains(@class, 'container')]/p[contains(text(), 'You have reached the maximum allowed access')]");
            if (alertNode != null)
            {
                throw new MaximumAccessReachedException("Alcanzó el límite máximo. Por favor intente mañana.");
            }

            // Obtener compañías existentes en la base de datos
            HashSet<string> existingCompanies = GetExistingCompanies(countryCode);

            HashSet<string> pageCompanies = new HashSet<string>();
            bool hasMoreResults = false;

            foreach (var nodo in doc.DocumentNode.CssSelect(".directoyname"))
            {
                var nodoAnchor = nodo.CssSelect("a").FirstOrDefault();

                if (nodoAnchor != null)
                {
                    string companyName = nodoAnchor.InnerText.Trim();
                    string href = nodoAnchor.GetAttributeValue("href", string.Empty);
                    string fullUrl = href.StartsWith("http") ? href : new Uri(new Uri("https://www.wcaworld.com"), href).ToString();

                    pageCompanies.Add(companyName); // Agregar a la lista de compañías de la página

                    if (!existingCompanies.Contains(companyName))
                    {
                        // Insertar solo si no existe
                        await ExtractCompanyDetails(companyName, fullUrl, countryCode);
                    }
                }
            }

            // Si los datos coinciden completamente, borra y reinserta
            if (existingCompanies.SetEquals(pageCompanies))
            {
                Console.WriteLine("Los datos coinciden completamente, Actualizando.");
               

                foreach (var nodo in doc.DocumentNode.CssSelect(".directoyname"))
                {
                    var nodoAnchor = nodo.CssSelect("a").FirstOrDefault();
                    if (nodoAnchor != null)
                    {
                        string companyName = nodoAnchor.InnerText.Trim();
                        string href = nodoAnchor.GetAttributeValue("href", string.Empty);
                        string fullUrl = href.StartsWith("http") ? href : new Uri(new Uri("https://www.wcaworld.com"), href).ToString();

                        await ExtractCompanyDetails(companyName, fullUrl, countryCode);
                    }
                }
            }
            var loadMoreNode = doc.DocumentNode.SelectSingleNode("//a[contains(@class, 'loadmore')]");
            hasMoreResults = loadMoreNode != null;

            return hasMoreResults;
        }
        private HashSet<string> GetExistingCompanies(string countryCode)
        {
            HashSet<string> existingCompanies = new HashSet<string>();
            string connectionString = _configuration.GetConnectionString("WebScrapingContext");
            string query = "SELECT \"Name\" FROM \"Company\" WHERE \"Pais\" = @Pais";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pais", countryCode);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            existingCompanies.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return existingCompanies;
        }
        private void DeleteExistingRecords(string countryCode, string source)
        {
            // Obtener la cadena de conexión desde appsettings.json
            string connectionString = _configuration.GetConnectionString("WebScrapingContext");
            string deleteQuery = "DELETE FROM \"Company\" WHERE \"Pais\" = @Pais AND \"Source\" = @Source";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var deleteCommand = new NpgsqlCommand(deleteQuery, connection))
                {
                    deleteCommand.Parameters.AddWithValue("@Pais", countryCode);
                    deleteCommand.Parameters.AddWithValue("@Source", source);
                    deleteCommand.ExecuteNonQuery();
                }
            }
        }
        private async Task ScrapeAllPagesForGLAAsync(string countryCode)
        {
            string baseUrl = $"https://www.glafamily.com/member/public/index/directory/index.html?country={countryCode}";
            string currentUrl = baseUrl;

            while (true)
            {
                _driver.Navigate().GoToUrl(currentUrl);
                await Task.Delay(5000); // Espera para asegurar que la página se cargue

                var pageContent = _driver.PageSource;
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(pageContent);

                // Verificar si se alcanzó el límite de acceso
                var alertNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'alert-danger') and contains(text(), 'You have reached the maximum allowed access')]");
                if (alertNode != null)
                {
                    throw new MaximumAccessReachedException("Alcanzó el límite máximo. Por favor intente mañana.");
                }

                // Seleccionar cada empresa y navegar a su página de detalles
                var companyNodes = doc.DocumentNode.SelectNodes("//div[@class='item']//div[@class='title']");
                if (companyNodes == null || companyNodes.Count == 0)
                {
                    Console.WriteLine("No se encontraron empresas en esta página.");
                    break; // Si no hay empresas, termina el bucle
                }

                foreach (var companyNode in companyNodes)
                {
                    string companyName = companyNode.InnerText.Trim();
                    string href = companyNode.ParentNode.ParentNode.GetAttributeValue("data-id", string.Empty);
                    string fullUrl = $"https://www.glafamily.com/member/public/index/company/index.html?id={href}";

                    Console.WriteLine($"Procesando empresa: {companyName}");
                    await ExtractCompanyDetails2(companyName, fullUrl, countryCode);
                }

                // Verificar el enlace para la siguiente página
                var currentPageNode = doc.DocumentNode.SelectSingleNode("//div[@class='pagination']/a[@class='cur']");
                if (currentPageNode == null)
                {
                    Console.WriteLine("No se pudo determinar la página actual.");
                    break;
                }

                int currentPage = int.Parse(currentPageNode.InnerText.Trim());
                var nextPageNode = doc.DocumentNode.SelectSingleNode($"//div[@class='pagination']/a[@title=' {currentPage + 1} ']");
                if (nextPageNode == null)
                {
                    Console.WriteLine("No hay más páginas para procesar.");
                    break;
                }

                // Obtener el atributo href relativo para la siguiente página
                string nextPageHref = nextPageNode.GetAttributeValue("href", string.Empty);
                if (string.IsNullOrEmpty(nextPageHref))
                {
                    Console.WriteLine("No se encontró el enlace para la siguiente página.");
                    break;
                }

                // Construir la URL completa para la siguiente página
                currentUrl = new Uri(new Uri(baseUrl), nextPageHref).ToString();
                Console.WriteLine($"Navegando a la siguiente página: {currentUrl}");
            }
        }
        private async Task ScrapeAllPagesForJCTRANSAsync(string countryCode)
        {
            string baseUrl = $"https://www.jctrans.com/en/membership/listc/{countryCode}/0-0?years=0&page=1";
            string currentUrl = baseUrl;

            try
            {
                // Cargar la primera página para determinar el número total de páginas
                _driver.Navigate().GoToUrl(currentUrl);
                await Task.Delay(15000); // Espera para asegurar que la página se cargue

                var initialPageContent = _driver.PageSource;
                HtmlDocument initialDoc = new HtmlDocument();
                initialDoc.LoadHtml(initialPageContent);

                // Intentar extraer el número total de páginas
                var lastPageNode = initialDoc.DocumentNode.SelectSingleNode("//ul[contains(@class, 'el-pager')]/li[last()]");
                int totalPages = 1; // Asumir que solo hay una página por defecto

                if (lastPageNode != null)
                {
                    if (int.TryParse(lastPageNode.InnerText.Trim(), out int parsedPages))
                    {
                        totalPages = parsedPages;
                    }
                }
                else
                {
                    Console.WriteLine($"No se encontró el número total de páginas para {countryCode}. Asumiendo una sola página.");
                }

                Console.WriteLine($"Número total de páginas: {totalPages}");

                // Iterar sobre todas las páginas
                for (int currentPage = 1; currentPage <= totalPages; currentPage++)
                {
                    currentUrl = baseUrl.Replace("page=1", $"page={currentPage}");
                    Console.WriteLine($"Navegando a la página {currentPage}: {currentUrl}");

                    _driver.Navigate().GoToUrl(currentUrl);
                    await Task.Delay(15000); // Espera para asegurar que la página se cargue

                    var pageContent = _driver.PageSource;
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(pageContent);

                    // Verificar si se alcanzó el límite de acceso
                    var alertNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'alert-danger') and contains(text(), 'You have reached the maximum allowed access')]");
                    if (alertNode != null)
                    {
                        throw new MaximumAccessReachedException("Alcanzó el límite máximo. Por favor intente mañana.");
                    }

                    // Seleccionar cada empresa y navegar a su página de detalles
                    var companyNodes = doc.DocumentNode.SelectNodes("//ul[contains(@class, 'membership-list-content-center-list')]/li");
                    if (companyNodes == null || companyNodes.Count == 0)
                    {
                        Console.WriteLine("No se encontraron empresas en esta página.");
                        continue; // Pasar a la siguiente página
                    }

                    foreach (var companyNode in companyNodes)
                    {
                        var companyNameNode = companyNode.SelectSingleNode(".//div[contains(@class, 'membership-list-content-center-list-item-left-top-content-title')]/p");
                        var cityNode = companyNode.SelectSingleNode(".//div[contains(@class, 'membership-list-content-center-list-item-left-top-content-location')]/span");

                        string companyName = companyNameNode?.InnerText.Trim() ?? "Unknown Company";
                        string city = cityNode?.InnerText.Trim() ?? "Unknown City";
                        string fullCompanyName = $"{companyName} - {city}";

                        var linkNode = companyNode.SelectSingleNode(".//a[@href]");
                        string href = linkNode?.GetAttributeValue("href", string.Empty);

                        if (!string.IsNullOrEmpty(href))
                        {
                            string fullUrl = new Uri(new Uri("https://www.jctrans.com"), href).ToString();
                            Console.WriteLine($"Procesando empresa: {fullCompanyName} - URL: {fullUrl}");
                            await ExtractCompanyDetails3(fullCompanyName, fullUrl, countryCode);
                        }
                    }
                }

                Console.WriteLine("Se procesaron todas las páginas exitosamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error procesando las páginas: {ex.Message}");
            }
            finally
            {
                // Borrar cookies y almacenamiento al final del proceso
                try
                {
                    Console.WriteLine("Borrando cookies y caché...");
                    _driver.Manage().Cookies.DeleteAllCookies();
                    ((IJavaScriptExecutor)_driver).ExecuteScript("window.localStorage.clear();");
                    ((IJavaScriptExecutor)_driver).ExecuteScript("window.sessionStorage.clear();");
                    Console.WriteLine("Cookies y caché borrados exitosamente.");
                }
                catch (Exception cleanupEx)
                {
                    Console.WriteLine($"Error al borrar cookies o caché: {cleanupEx.Message}");
                }
            }
        }

        private async Task ExtractCompanyDetails(string companyName, string url, string countryCode)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Console.WriteLine($"La URL para {companyName} está vacía. Saltando...");
                return;
            }
            Uri targetUri = new Uri(url);
            string baseDomain = "https://www.wcaworld.com";

            // Verifica si cambió el dominio y reinicia sesión si es necesario
            if (targetUri.Host != new Uri(baseDomain).Host)
            {
                // Reiniciar sesión o restaurar cookies al cambiar de dominio
                Console.WriteLine("Dominio cambiado. Reestableciendo sesión en " + targetUri.Host);
                _driver.Manage().Cookies.DeleteAllCookies();
                await LoginWithSeleniumAsync(); // Opcionalmente, restablece la sesión en el nuevo dominio si es necesario
            }

            // Continuar con la extracción de datos después de manejar el cambio de dominio
            _driver.Navigate().GoToUrl(url);
            await Task.Delay(15000); // Mantén el tiempo de espera

            var pageContent = _driver.PageSource;
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(pageContent);

            // Verificar si el mensaje de error está presente en la página del miembro
            var alertNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'alert-danger') and contains(@class, 'text-center') and contains(@class, 'container')]/p[contains(text(), 'You have reached the maximum allowed access')]");
            if (alertNode != null)
            {
                throw new MaximumAccessReachedException("Alcanzó el límite máximo. Por favor intente mañana.");
            }

            string address = "No Address Available";
            string mainPhone = "No Phone Available";
            string website = "No Website Available";
            string mainEmail = "No Email Available";
            HashSet<string> additionalEmails = new HashSet<string>();

            // Extraer Address
            var addressNode = doc.DocumentNode.SelectSingleNode("//div[@class='col-md-12']/span");
            if (addressNode != null)
            {
                address = addressNode.InnerHtml.Replace("<br>", ", ").Trim();
            }

            // Extraer detalles de contacto (Phone, Website, Email)
            var contactNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'profile_row')]");
            if (contactNodes != null)
            {
                foreach (var node in contactNodes)
                {
                    var labelNode = node.SelectSingleNode(".//div[contains(@class, 'profile_label')]");
                    var valueNode = node.SelectSingleNode(".//div[contains(@class, 'profile_val')]");
                    if (labelNode != null && valueNode != null)
                    {
                        string label = labelNode.InnerText.Trim().TrimEnd(':');
                        string value = valueNode.InnerText.Trim();

                        if (label == "Phone" && mainPhone == "No Phone Available")
                        {
                            mainPhone = value;
                        }
                        else if (label == "Website")
                        {
                            var websiteLink = valueNode.SelectSingleNode(".//a");
                            website = websiteLink != null ? websiteLink.GetAttributeValue("href", "No Website Available") : "No Website Available";
                        }
                        else if (label == "Email")
                        {
                            var emailLink = valueNode.SelectSingleNode(".//a");
                            string email = emailLink != null ? emailLink.InnerText.Trim() : "No Email Available";
                            if (mainEmail == "No Email Available")
                            {
                                mainEmail = email;
                            }
                            additionalEmails.Add(email);
                        }
                    }
                }
            }

            // Extraer contactos en Office Contacts
            List<string> nameContacts = new List<string>();
            List<string> titleContacts = new List<string>();
            List<string> emailContacts = new List<string>();

            var officeContactNodes = doc.DocumentNode.SelectNodes("//div[@class='contactperson_info row col-xs-12 col-sm-9']");
            if (officeContactNodes != null)
            {
                foreach (var contactNode in officeContactNodes)
                {
                    var nameNode = contactNode.SelectSingleNode(".//div[contains(@class, 'profile_label') and contains(text(), 'Name:')]/following-sibling::div");
                    var titleNode = contactNode.SelectSingleNode(".//div[contains(@class, 'profile_label') and contains(text(), 'Title:')]/following-sibling::div");
                    var emailNode = contactNode.SelectSingleNode(".//div[contains(@class, 'profile_label') and contains(text(), 'Email:')]/following-sibling::div/a");

                    string nameContact = nameNode?.InnerText.Trim() ?? "No Contact Name";
                    string titleContact = titleNode?.InnerText.Trim() ?? "No Title";
                    string emailContact = emailNode?.InnerText.Trim() ?? "No Email Available";

                    nameContacts.Add(nameContact);
                    titleContacts.Add(titleContact);
                    emailContacts.Add(emailContact);
                }
            }

            // Convertir las listas en cadenas concatenadas
            string nameContactsStr = string.Join(", ", nameContacts);
            string titleContactsStr = string.Join(", ", titleContacts);
            string emailContactsStr = string.Join(", ", emailContacts);

            string additionalEmailsStr = additionalEmails.Count > 1 ? string.Join(", ", additionalEmails) : mainEmail;

            // Inserción de datos en la base de datos
            InsertIntoDatabase(
                companyName,
                url,
                address,
                mainPhone,
                website,
                 _source,
                countryCode,
                nameContactsStr,
                mainEmail,
                titleContactsStr,
                emailContactsStr,
                mainPhone
            );
        }

        private async Task ExtractCompanyDetails2(string companyName, string url, string countryCode)
        {
            _driver.Navigate().GoToUrl(url);
            await Task.Delay(15000); // Tiempo de espera

            var pageContent = _driver.PageSource;
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(pageContent);

            // Verificar si se alcanzó el límite de acceso
            var alertNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'alert-danger') and contains(text(), 'You have reached the maximum allowed access')]");
            if (alertNode != null)
            {
                throw new MaximumAccessReachedException("Alcanzó el límite máximo. Por favor intente mañana.");
            }

            // Extracción de los detalles de la empresa
            string address = doc.DocumentNode.SelectSingleNode("//div[@class='item' and div[@class='label' and contains(., 'Address:')]]//div[@class='value']")?.InnerText.Trim() ?? "No Address Available";
            string website = doc.DocumentNode.SelectSingleNode("//div[@class='item' and div[@class='label' and contains(normalize-space(.), 'Website:')]]//div[@class='value']/a")?.GetAttributeValue("href", "No Website Available") ?? "No Website Available";
            string mainPhone = doc.DocumentNode.SelectSingleNode("//div[@class='item' and div[@class='label' and contains(normalize-space(.), 'Office tel number:')]]//div[@class='value']")?.InnerText.Replace("\n", "").Replace(" ", "").Trim() ?? "No Phone Available";
            string mainEmail = doc.DocumentNode.SelectSingleNode("//div[@class='item' and div[@class='label' and contains(normalize-space(.), 'Office email:')]]//div[@class='value']")?.InnerText.Trim() ?? "No Email Available";


            List<string> nameContacts = new List<string>();
            List<string> titleContacts = new List<string>();
            List<string> emailContacts = new List<string>();
            List<string> mobileContacts = new List<string>();

            var contactNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'swiper-slide')]");
            if (contactNodes != null)
            {
                foreach (var contactNode in contactNodes)
                {
                    // Extraer los valores únicamente si existen
                    string nameContact = contactNode.SelectSingleNode(".//p[label='Name:']/text()")?.InnerText.Trim();
                    string titleContact = contactNode.SelectSingleNode(".//div[@class='top']/h2")?.InnerText.Trim();
                    string emailContact = contactNode.SelectSingleNode(".//p[label='Email:']/text()")?.InnerText.Trim();
                    string mobileContact = contactNode.SelectSingleNode(".//p[label='Mobile:']/text()")?.InnerText.Replace("\n", "").Replace(" ", "").Trim();

                    // Solo agregar si existen valores válidos
                    if (!string.IsNullOrEmpty(nameContact))
                        nameContacts.Add(nameContact);
                    if (!string.IsNullOrEmpty(titleContact))
                        titleContacts.Add(titleContact);
                    if (!string.IsNullOrEmpty(emailContact))
                        emailContacts.Add(emailContact);
                    if (!string.IsNullOrEmpty(mobileContact))
                        mobileContacts.Add(mobileContact);
                }
            }

            // Convertir listas en cadenas concatenadas
            string nameContactsStr = string.Join(", ", nameContacts);
            string titleContactsStr = string.Join(", ", titleContacts);
            string emailContactsStr = string.Join(", ", emailContacts);
            string mobileContactsStr = string.Join(", ", mobileContacts);

            // Insertar en la base de datos
            InsertIntoDatabase(
                companyName.Trim(),
                url,
                address,
                mainPhone,
                website,
                _source,
                countryCode,
                nameContactsStr,
                mainEmail,
                titleContactsStr,
                emailContactsStr,
                mobileContactsStr
            );

        }
        private async Task ExtractCompanyDetails3(string companyName, string url, string countryCode)
        {
            _driver.Navigate().GoToUrl(url);
            await Task.Delay(15000); // Tiempo de espera para la carga

            var pageContent = _driver.PageSource;
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(pageContent);

            // Verificar si se alcanzó el límite de acceso
            var alertNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'alert-danger') and contains(text(), 'You have reached the maximum allowed access')]");
            if (alertNode != null)
            {
                throw new MaximumAccessReachedException("Alcanzó el límite máximo. Por favor intente mañana.");
            }

            // Extracción de los detalles de la empresa
            string address = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'address_view')]")?.InnerText.Trim() ?? "No Address Available";
            string website = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'el-link__inner')]")?.InnerText.Trim() ?? "No Website Available";

            List<string> nameContacts = new List<string>();
            List<string> titleContacts = new List<string>();
            List<string> emailContacts = new List<string>();
            List<string> mobileContacts = new List<string>();

            var contactNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'contact-item')]");
            if (contactNodes != null)
            {
                foreach (var contactNode in contactNodes)
                {
                    string nameContact = contactNode.SelectSingleNode(".//span[contains(@class, 'contact-name')]")?.InnerText.Trim();
                    string titleContact = contactNode.SelectSingleNode(".//span[contains(@class, 'contact-title')]")?.InnerText.Trim();
                    string emailContact = contactNode.SelectSingleNode(".//span[contains(@class, 'contact-email')]")?.InnerText.Trim();
                    string mobileContact = contactNode.SelectSingleNode(".//span[contains(@class, 'contact-mobile')]")?.InnerText.Trim();

                    if (!string.IsNullOrEmpty(nameContact)) nameContacts.Add(nameContact);
                    if (!string.IsNullOrEmpty(titleContact)) titleContacts.Add(titleContact);
                    if (!string.IsNullOrEmpty(emailContact)) emailContacts.Add(emailContact);
                    if (!string.IsNullOrEmpty(mobileContact)) mobileContacts.Add(mobileContact);
                }
            }

            // Convertir listas en cadenas concatenadas
            string nameContactsStr = string.Join(", ", nameContacts);
            string titleContactsStr = string.Join(", ", titleContacts);
            string emailContactsStr = string.Join(", ", emailContacts);
            string mobileContactsStr = string.Join(", ", mobileContacts);

            // Insertar en la base de datos
            InsertIntoDatabase(
                companyName.Trim(),
                url,
                address,
                null, // Teléfono principal no disponible en esta página
                website,
                _source,
                countryCode,
                nameContactsStr,
                null, // Email principal no disponible en esta página
                titleContactsStr,
                emailContactsStr,
                mobileContactsStr
            );
        }



        private void InsertIntoDatabase(string companyName, string href, string address, string phone, string website, string source, string pais, string nameContact, string email, string titleContact, string emailContact, string mobile)
        {
            string connectionString = _configuration.GetConnectionString("WebScrapingContext");
            string query = @"
                INSERT INTO ""Company"" (""Name"", ""Href"", ""Address"", ""Phone"", ""Website"", ""Source"", ""Pais"", ""NameContact"", ""Email"", ""Titlecontact"", ""Emailcontact"", ""Mobile"", ""Fecha"")
                VALUES (@Name, @Href, @Address, @Phone, @Website, @Source, @Pais, @NameContact, @Email, @Titlecontact, @Emailcontact, @Mobile, @Fecha)
                ON CONFLICT (""Name"", ""Pais"", ""Source"") DO UPDATE
                SET ""Href"" = @Href, 
                    ""Address"" = @Address, 
                    ""Phone"" = @Phone, 
                    ""Website"" = @Website, 
                    ""NameContact"" = @NameContact, 
                    ""Email"" = @Email, 
                    ""Titlecontact"" = @Titlecontact, 
                    ""Emailcontact"" = @Emailcontact, 
                    ""Mobile"" = @Mobile, 
                    ""Fecha"" = @Fecha;
            ";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", companyName ?? string.Empty);
                    command.Parameters.AddWithValue("@Href", href ?? string.Empty);
                    command.Parameters.AddWithValue("@Address", address ?? string.Empty);
                    command.Parameters.AddWithValue("@Phone", phone ?? "No Phone Available");
                    command.Parameters.AddWithValue("@Website", website ?? "No Website Available");
                    command.Parameters.AddWithValue("@Source", source ?? string.Empty);
                    command.Parameters.AddWithValue("@Pais", pais ?? string.Empty);
                    command.Parameters.AddWithValue("@NameContact", nameContact ?? string.Empty);
                    command.Parameters.AddWithValue("@Email", email ?? string.Empty);
                    command.Parameters.AddWithValue("@Titlecontact", titleContact ?? string.Empty);
                    command.Parameters.AddWithValue("@Emailcontact", emailContact ?? string.Empty);
                    command.Parameters.AddWithValue("@Mobile", mobile ?? "No Mobile Available");
                    command.Parameters.AddWithValue("@Fecha", DateTime.UtcNow);

                    command.ExecuteNonQuery();
                }
            }
        }


    }
}
