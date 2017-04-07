# Azure Usage and Billing Portal
#
# Please read all the comments below. Default values of some variables such as SQL Server password etc. needs update to prevent any security issue.
#
# This script will create an Azure Resource group with WebSite, AzureStorage, AzureSQLDB services under it. It will create basic tier services and dont forget to update parameters to adjust the service tier to a free one (which may create performance issues).
# Finally it will create an Active Directory (AD) Application which needs some manual settings to be done under the Azure clasic (OLD) portal.
# !!! Note the AD App name below (defined in the below script params). You have to update the ad app with that name, no other AD apps. !!!
#
# This script is working together with "resources.json" ARM Template file. Be sure the file exist in the same path or
# Set the below variables with your own parameters.
#
# !!! Before running the script, you should change the current working directory to be same as the script directory !!!
#
# You may refer to "How to Setup the Azure Usage & Billing Portal" https://channel9.msdn.com/blogs/Mustafa-Kasap/How-to-Setup-the-Azure-Usage--Billing-Portal video tutorial which shows every single piece of installation steps with the current repo files.


### 0. Parameters that will be used to create the resouces. WorkingDir must contain this PS1 and depending Json file
$WorkingDir = ".\"
$TemplateFileName = "CreateAzureServicesScriptResources.json"
$TemplateFileFullPath = $WorkingDir + $TemplateFileName

# If you have more than one subscription, please specify the name of the subscription that you want to use among them.
# If you are not sure how many subscriptions you have and want to use the default one, than keep it this parameter as $AzureSubscriptionName = ""
$AzureSubscriptionName = "Visual Studio Ultimate with MSDN"

# identification / version suffix in service names. Probably one another user is using this value too. Make it unique! (i.e. with your initials)
$suffix = "20"

# Azure resource group parameters
$ResourceGroupName = ("aui-resource-group" + $suffix)
$ResourceGroupLocation = "Central US"

# Storage account parameters
$StorageAccountName = ("auistorage" + $suffix)
$StorageAccountType = "Standard_LRS"
$StorageAccountLocation = $ResourceGroupLocation

# AzureSQL Server parameters
# This will create an AzureSQL Standard S0 instance please review https://azure.microsoft.com/pricing/details/sql-database/ for price details
$SqlServerName = ("auisqlsr" + $suffix)
$SqlServerLocation = $ResourceGroupLocation
$SQLServerVersion = "2.0"
$SqlAdministratorLogin = "mksa"
$SqlAdministratorLoginPassword = "Password.1%"
$SqlDatabaseName = ("auisqldb" + $suffix)
$sqlEdition = "Standard"
$sqlRequestedServiceObjectiveName = "S0";

# WebSite parameters
$Web1SiteName = ("auiregistration" + $suffix)
$Web2SiteName = ("auidashboard" + $suffix)
$WebHostingPlanName = ("auihostingplan" + $suffix)
$WebSiteLocation = $ResourceGroupLocation
$webSiteInsightsLocation = "South Central US"

# Active Directory (AD) Application parameters
# You have to find and update the AD App with below name on the classic portal
$displayName1 = ("Azure Usage and Billing Portal (Registration) v" + $suffix)
$passwordADApp = "Password.1%"


### 1. Login to Azure Resource Manager service. Credentials will be stored under this session for furthure use
#############################################################################################
Login-AzureRmAccount

Get-AzureRmSubscription –SubscriptionName $AzureSubscriptionName | Select-AzureRmSubscription

### 2. Create a Resource Group. All resources will be created under this group
#############################################################################################
$ResourceGroup = @{
    Name = $ResourceGroupName;
    Location = $ResourceGroupLocation;
    Force = $true;
};
New-AzureRmResourceGroup @ResourceGroup;


### 3. Create resources: Web apps, SQL Server, Storage
#############################################################################################
$ResourceParameters = @{
    # storage parameters
    storageAccountName = $StorageAccountName;
    storageAccountType = $StorageAccountType;
    storageAccountLocation = $StorageAccountLocation;

    # sql server parameters
    sqlServerName = $SqlServerName;
    sqlServerLocation =  $SqlServerLocation;
    sqlServerVersion = $SQLServerVersion;
    sqlAdministratorLogin = $SqlAdministratorLogin;
    sqlAdministratorLoginPassword = $SqlAdministratorLoginPassword;
    sqlDatabaseName = $SqlDatabaseName;
    sqlCollation = "SQL_Latin1_General_CP1_CI_AS";
    sqlEdition = $sqlEdition;
    sqlMaxSizeBytes = "1073741824";
	sqlRequestedServiceObjectiveName = $sqlRequestedServiceObjectiveName;

    # website parameters
    registrationWebSiteName = $Web1SiteName;
    dashboardWebSiteName = $Web2SiteName;
    webHostingPlanName = $WebHostingPlanName;
    webSiteLocation = $WebSiteLocation;
    webSiteInsightsLocation = $webSiteInsightsLocation;
    webSku = "Basic";
    webWorkerSize = "1";
};
New-AzureRmResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateFile $TemplateFileFullPath -TemplateParameterObject $ResourceParameters -Verbose

### 4. Create Azure Active Directory apps in default directory
### MICROSOFT FTE should not use this section to create the AD app under default AD. They need to create
### a new AD and create the AD app under it.
#############################################################################################
$u = (Get-AzureRmContext).Account
$u1 = ($u -split '@')[0]
$u2 = ($u -split '@')[1]
$u3 = ($u2 -split '\.')[0]
$defaultPrincipal = ($u1 + $u3 + ".onmicrosoft.com")

# Create Active Directory Application
$homePageURL1 = ("http://" + $Web1SiteName + ".azurewebsites.net")
$identifierURI1 = ("http://" + $defaultPrincipal + "/" + $Web1SiteName)
$azureAdApplication1 = New-AzureRmADApplication -DisplayName $displayName1 -HomePage $homePageURL1 -IdentifierUris $identifierURI1 -Password $passwordADApp


### 5. Print out the required project settings parameters
#############################################################################################
# Get storage account key
$storageKey = Get-AzureRmStorageAccountKey -Name $StorageAccountName -ResourceGroupName $ResourceGroupName

# Get tenant ID
$tenantID = (Get-AzureRmContext).Tenant.TenantId
# This value is manually set in AD Application settins. Get that value from the portal, if not set you can set it as your HomePageURL
$PostLogoutRedirectUri1 = $homePageURL1

Write-Host ("Parameters to be used in the project settings / configuration files.") -foreground Green
Write-Host ("Please update parameters in Web.config and App.config with the ones below.") -foreground Green
Write-Host ("====================================================================`n") -foreground Green

Write-Host "Commons: " -foreground Yellow
Write-Host "`tCommons.cs: " -foreground Yellow
Write-Host "`tConnection string name is fixed at line: " -foreground Green -NoNewLine
Write-Host "public DataAccess() : base('ASQLConn') { } " -foreground Red -NoNewLine
Write-Host "do not change it. It reflects the database name to be created under the SQL Server" -foreground Green
Write-Host ""
Write-Host "ida:ClientID: " -foreground Green –NoNewLine
Write-Host $azureAdApplication1.ApplicationId -foreground Red 
Write-Host "ida:Password: " -foreground Green –NoNewLine
Write-Host $passwordADApp -foreground Red 
Write-Host "ida:PostLogoutRedirectUri: " -foreground Green –NoNewLine
Write-Host $PostLogoutRedirectUri1 -foreground Red 
Write-Host "ASQLConn ConnectionString: " -foreground Green –NoNewLine
Write-Host ("Data Source=tcp:" + $SqlServerName + ".database.windows.net,1433;Initial Catalog=" + $SqlDatabaseName + ";User Id=" + $SqlAdministratorLogin + "@" + $SqlServerName + ";Password="+ $SqlAdministratorLoginPassword +";") -foreground Red 
Write-Host "ida:TenantId: " -foreground Green –NoNewLine
Write-Host $tenantID -foreground Red 
Write-Host "AzureWebJobsDashboard: " -foreground Green –NoNewLine
Write-Host ("DefaultEndpointsProtocol=https;AccountName=" + $StorageAccountName + ";AccountKey=" + $storageKey[0].value) -foreground Red 
Write-Host "AzureWebJobsStorage: " -foreground Green –NoNewLine
Write-Host ("DefaultEndpointsProtocol=https;AccountName=" + $StorageAccountName + ";AccountKey=" + $storageKey[0].value) -foreground Red 

Write-Host ("Some manuel settings to be done!") -foreground Yellow

Write-Host ("- Update '") -foreground Yellow –NoNewLine
Write-Host $displayName1 -foreground Red –NoNewLine
Write-Host ("' Active Directory Application (AD App) settings! (see README.md)") -foreground Yellow

Write-Host ("- On the configuration page of the AD App, find the section name 'Reply URL' and add following addresses: ") -foreground Yellow –NoNewLine
Write-Host $PostLogoutRedirectUri1 -foreground Red –NoNewLine
Write-Host ("with http and https. Also add http://localhost in case debugging locally.") -foreground Yellow

Write-Host ("- Update ProcessQueueMessage function input parameters in WebJob project, functions.cs file to be same as 'ida:QueueBillingDataRequests' param value in webconfig files.") -foreground Yellow
Write-Host ("- ida:QueueReportRequest & ida:BlobReportPublish parameters in Web.Config (Dashboard) & App.Config(WebJob) files must be same.") -foreground Yellow
