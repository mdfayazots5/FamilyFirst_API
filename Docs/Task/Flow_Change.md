



select * from tblModule

select * from tblSubModule

select * from tblStaticAPITemplate

select * from tblMasterData

select * from tblModulePermission

select * from tblRolePermission

select * from tblRole

select * from tblPermission

select * from tblAlertType

select * from tblSMSProvider

select * from tblSMSTemplate

select * from tblSMSLog

select * from tblStoreEmailTemplate

select * from tblStoreEmailLog

select * from tblEmailSMTDetail

select * from tblWhatsAppLog

select * from tblWhatsAppProvider

select * from tblWhatsAppTemplate

select * from tblNotificationLog

select * from tblNotificationTemplate

select * from tblNotificationProviderType

select * from tblUser

select * from tblUserType

select * from tblErrorCode

select * from tblregularexpresion

select * from tblPincode

select * from tblCountry

select * from tblState

select * from tblCity

select * from tblAPIMethod


usp_MasterData_GetMasterDataByCode

how many masters getting from the ui, those all tables must be should insert in the masterdata table, then from that the api will get the data, on ui should get the GUID's only and codes or names should get, no primary id will hide or not get to ui, then from any saving the selected id's will pass, then via the usp_MasterData_GetMasterDataByCodeInternal should validate those id's in apilevel, then newxt from the api those returned primary id's will pass to save sp's. this is the functinaliy. 

rule is anywhere in ui level no primary id's will pass, only return the guid's only. all this tabls should maintain in the api as enums, then easy to pass to sp, so this table name, from sp, find that table and pass this id, verify, then return those ids. 


for the each save api wise tblAPIMethod, create a new table for regularexpresions, where is the required the regular expresion to this api's those all regurlar expresions must be create and insert the data, then next when the project starting all those regelar expression must get and store in the redis catchi. think like a senior archtehc then where is the requird think, analsys, then create a redis resuble code, for use all like regular expreseion, masterdata, errorcodes, master data by code, like etc... where is crttical leave those data. 

every error codes or every regular expresion must get from db or redis chatch. for get those type of data, for each errorcodes, or regular exprestion, masterdata, tables, etc,.... enums maintain the the api level, then easy to each api wise filed wise selected enum will pass and get the data.







1. in the present project for the user what is the not required thigns, those we are doing like offline, in the present anywhere this is not using, then why we are using, we are doing unsarry things, then we don't want this type of features, first find this type of features, think like a senior full statck developerr and senior app analsys, senior project manager, this type of things is not required, then why we are using this type of things. please think as a senior and find this fearues, and give me when whare, why, how those features are will use, then i will finalize this is it required or not. 
and sbucripns remove it, not required for us. 





#region UpdateDMSStatusById
//***************************************************************************************************
// Layer                        :   BAL 
// Method Name                  :   UpdateDMSStatusById
// Method Description           :   This method is used to Update DMS Status By Id.
// Author                       :   Jafar Sadik
// Creation Date                :   10 Oct 2024
// Input Parameters             :   objAPIRequest
// Modified Date                : 
// Modified Reason              :
// Return Values                :   objResponse
//----------------------------------------------------------------------------------------------------
//  Version               Author                            Date                        Remarks       
// ---------------------------------------------------------------------------------------------------
//  1.0                 Jafar Sadik                     10 Oct 2024                   Creation
//  1.1                 Prakash T                      03 Oct 2025                   RDLC - REV0036/MS/RDLC/865 -Common dll separation changes - Revalsys.SAASPOSCommon Reference Added
//                                                                                   and using SAASPOSGeneral for enums 
//****************************************************************************************************
/// <summary>
/// <c>UpdateDMSStatusById</c> This method is used to Update DMS Status By Id.
/// <param>objAPIRequest</param>
/// <returns>objResponse</returns> objResponse
/// </summary>
/// 
public Response<object> UpdateDMSStatusById(DMSStatusRequestListDTO objAPIRequest)
{
    DateTime startTime = DateTime.MinValue; bool IsMicrosoftInsightsRequired = false;
    Stopwatch timer = null; object objApplicationInsights = null;

    startTime = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes);
    timer = System.Diagnostics.Stopwatch.StartNew();

    #region Common  Variables
    SAASPOSRegularExpressions objRegularExpressions = null;
    APILogDetailListDTO objAPILogDetailListDTO = null;
    List<ErrorCodeListDTO> lstErrorCodeListDTO = null;
    DateTime startResponseTime = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes);
    Response<object> objResponse = null;
    string strCustomerName = string.Empty, strCountryCode = string.Empty, strCurrenyCode = string.Empty, strLanguageCode = string.Empty, strIPAddress = string.Empty;
    int  intCountryId = 0, intCurrencyId = 0, intLanguageId = 0, ErrorCode = 0, intRoleId = 0; 
    string strMobile = string.Empty, strAlternateMobile = string.Empty, strEmail = string.Empty, strResponse = string.Empty, strCustomerGUID = string.Empty, strEnrollmentStoreGUId = String.Empty;
    bool IsAuthorizedUser = false;
    Int64  intUserId = 0,intSuccess = 0;
    DateTime dtTime = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes);
    
    #endregion

    #region  Specified variables
    string strRequest = string.Empty, strAddressId = string.Empty, strAddress = string.Empty, strAddressType = string.Empty, strStateCode = string.Empty, strGSTINNumber = string.Empty, strStoreName = string.Empty;
    MySqlDMSDAL objMySqlDMSDAL = null;
    string  strSiteToken = string.Empty, strMethodName = MethodBase.GetCurrentMethod().ToString(); 
    DMSDAL objDMSDAL = null;
    CommonBAL objCommonBAL = null;
    ErrorCodeBAL objErrorCodeBAL = null;
    DateTime dtTokenExpiryTime = DateTime.MinValue;
    DMSStatusListDTO objDMSStatusListDTO = null;
    object objDMSDocumentId = null, objDMSDMSStatusId = null;
    MasterBAL objMasterBAL = null;
    #endregion

    try
    {
        General.CreateCodeLog("Step 1.1", "Before Validating Update DMS Status Details", "", MethodBase.GetCurrentMethod().Name);

        objRegularExpressions = new SAASPOSRegularExpressions();

        strRequest = JsonConvert.SerializeObject(objAPIRequest);

        if (objAPIRequest != null)
        {
            General.CreateCodeLog("Step 1.2", "Before Validating Common Values", "", MethodBase.GetCurrentMethod().Name);

            #region common validation

            #region CountryCode validations

            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 1.3", "Before Validating CountryCode", "", MethodBase.GetCurrentMethod().Name);
                if (!string.IsNullOrEmpty(objAPIRequest.CountryCode))
                {
                    strCountryCode = objAPIRequest.CountryCode;

                    if (!string.IsNullOrEmpty(strCountryCode))
                    {
                        intCountryId = General.GetCountryId(strCountryCode);
                    }
                    if (intCountryId <= 0)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Country_Code);
                    }
                }
                General.CreateCodeLog("Step 1.4", "After Validating CountryCode", "", MethodBase.GetCurrentMethod().Name);
            }
            #endregion

            #region Curreny Code validations
            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 1.5", "Before Validating Currency Code", "", MethodBase.GetCurrentMethod().Name);
                if (!string.IsNullOrEmpty(objAPIRequest.CurrencyCode))
                {
                    strCurrenyCode = objAPIRequest.CurrencyCode;

                    if (!string.IsNullOrEmpty(strCurrenyCode))
                    {
                        intCurrencyId = General.GetCurrencyId(strCurrenyCode);
                    }

                    if (intCurrencyId <= 0)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Currency_Code);
                    }
                }
                General.CreateCodeLog("Step 1.6", "After Validating Currency Code", "", MethodBase.GetCurrentMethod().Name);

            }
            #endregion

            #region Language Code validations
            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 1.7", "Before Validating Language Code", "", MethodBase.GetCurrentMethod().Name);

                if (!string.IsNullOrEmpty(objAPIRequest.LanguageCode))
                {
                    strLanguageCode = objAPIRequest.LanguageCode;

                    if (!string.IsNullOrEmpty(strLanguageCode))
                    {
                        intLanguageId = General.GetCurrencyId(strLanguageCode);
                    }

                    if (intLanguageId <= 0)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Language_Code);
                    }
                }
                General.CreateCodeLog("Step 1.8", "After Validating Language Code", "", MethodBase.GetCurrentMethod().Name);

            }
            #endregion

            #region Assign IP Address

            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 1.9", "Assigning IPAddress", "", MethodBase.GetCurrentMethod().Name);
                if (!string.IsNullOrEmpty(objAPIRequest.IPAddress))
                {
                    strIPAddress = objAPIRequest.IPAddress;
                }
                else
                {
                    strIPAddress = General.GetIP4Address();
                    objAPIRequest.IPAddress = strIPAddress;
                }
            }
            #endregion

            General.CreateCodeLog("Step 1.10", "After Validating Common Values", "", MethodBase.GetCurrentMethod().Name);
            #endregion

            #region ValidateToken 
            if (ErrorCode == 0)
            {
                if (objAPIRequest.JwtToken != null && objAPIRequest.JwtToken != string.Empty)
                {
                    General.CreateCodeLog("Step 2.0", "Before calling ValidateToken", "", MethodBase.GetCurrentMethod().Name);

                    try
                    {
                        if (!string.IsNullOrEmpty(objAPIRequest.JwtToken))
                        {
                            General.CreateCodeLog("Step 2.1", "Before calling GetSiteDetails", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);

                            objCommonBAL = new CommonBAL(_objConfigurationSettingsListDTO);
                            lstSiteDetails = objCommonBAL.GetSiteDetails(objAPIRequest.JwtToken, intCountryId, intCurrencyId, intLanguageId);

                            General.CreateCodeLog("Step 2.2", "After calling GetSiteDetails", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                        }
                    }
                    catch (Exception ex)
                    {
                        General.objConfigurationSettingsListDTO = _objConfigurationSettingsListDTO;
                        General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                    }

                    if (lstSiteDetails != null && lstSiteDetails.Count > 0)
                    {
                        ErrorCode = lstSiteDetails[0].ReturnCode;

                        if (ErrorCode == 0)
                        {
                            strRespectiveConnectionString = lstSiteDetails[0].ConnectionString;
                            strSiteToken = lstSiteDetails[0].SecurityToken;
                            dtTokenExpiryTime = lstSiteDetails[0].TokenExpiryTime;
                            SiteCode = lstSiteDetails[0].SiteCode;
                            TimeZone = lstSiteDetails[0].TimeZone;
                            NoOfHours = lstSiteDetails[0].NoOfHours;
                            NoOfMinutes = lstSiteDetails[0].NoOfMinutes;
                        }
                    }
                    else
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Token);
                    }
                    General.CreateCodeLog("Step 2.3", "After calling ValidateToken", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Token_Required);
                }
            }
            #endregion

            #region GetSiteDetailByPrivateKey  
            if (ErrorCode == 0)
            {
                List<SiteUserListDTO> lstSiteDetailByPrivateKey = new List<SiteUserListDTO>();

                if (objAPIRequest.upk != null && objAPIRequest.upk != string.Empty)
                {
                    try
                    {
                        General.CreateCodeLog("Step 2.4", "Before calling GetSiteUserDetailByPrivateKey", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);

                        objCommonBAL = new CommonBAL(_objConfigurationSettingsListDTO, strRespectiveConnectionString);
                        lstSiteDetailByPrivateKey = objCommonBAL.GetSiteUserDetailByPrivateKey(objAPIRequest.upk, intCountryId, intCurrencyId, intLanguageId);

                        General.CreateCodeLog("Step 2.5", "After calling GetUserDetailByPrivateKey", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                    }
                    catch (Exception ex)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                        General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                    }
                }

                if (lstSiteDetailByPrivateKey != null && lstSiteDetailByPrivateKey.Count > 0)
                {
                    if (lstSiteDetailByPrivateKey[0].ExpiryDate > RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes))
                    {
                        intRoleId = lstSiteDetailByPrivateKey[0].RoleId;
                        strLoginUserName = lstSiteDetailByPrivateKey[0].FirstName + " " + lstSiteDetailByPrivateKey[0].LastName;
                        strLoginUserName = strLoginUserName.Trim();
                        intUserId = lstSiteDetailByPrivateKey[0].UserId;
                    }
                    else
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.User_Session_Expired);
                    }
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_User);
                }
            }
            #endregion GetSiteDetailByPrivateKey

            #region Permission Check
            if (ErrorCode == 0)
            {
                objCommonBAL = new CommonBAL(strRespectiveConnectionString, EncryptionKey, _objConfigurationSettingsListDTO);

                try
                {
                    General.CreateCodeLog("Step 2.6", "Before Calling CheckRolePermission", "", MethodBase.GetCurrentMethod().Name);
                    IsAuthorizedUser = objCommonBAL.CheckRolePermission(intRoleId, intModuleId, Convert.ToInt32(SAASPOSGeneral.Permissions.Approve_Reject), intCountryId, intCurrencyId, intLanguageId);
                    General.CreateCodeLog("Step 2.7", "After Calling CheckRolePermission", "", MethodBase.GetCurrentMethod().Name);
                }
                catch (Exception Ex)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                    General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                }
                finally
                {
                    objCommonBAL = null;
                }

                if (!IsAuthorizedUser)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Do_Not_Have_Permission);
                }
            }
            #endregion Permission Check

            #region GetSiteConfiguration
            if (ErrorCode == 0)
            {
                if (IsAuthorizedUser)
                {
                    objCommonBAL = new CommonBAL(_objConfigurationSettingsListDTO, lstSiteDetails[0].ConnectionString);
                    objApplicationInsights = objCommonBAL.GetSiteConfiguration(Convert.ToInt32(SAASPOSGeneral.SiteConfiguration.IsMicrosoftInsightsRequired), intCountryId, intLanguageId, intCurrencyId);

                    if (objApplicationInsights != null && !string.IsNullOrEmpty(objApplicationInsights.ToString()))
                    {
                        IsMicrosoftInsightsRequired = bool.Parse(objApplicationInsights.ToString());
                    }
                }
            }
            #endregion

            #region Request Validation
            General.CreateCodeLog("Step 2.8", "before validating the Request parameters", "", MethodBase.GetCurrentMethod().Name);

            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 2.9", "before validating DMSDocumentId", "", MethodBase.GetCurrentMethod().Name);

                if (!string.IsNullOrWhiteSpace(objAPIRequest.DMSDocumentId))
                {
                    objMasterBAL = new MasterBAL(_objConfigurationSettingsListDTO, strRespectiveConnectionString);
                    objDMSDocumentId = objMasterBAL.GetMasterDataByCodeInternalObject(SAASPOSGeneral.MasterDataCodes.DMSDocument.ToString(), objAPIRequest.DMSDocumentId.Trim(), lstSiteDetails, lstSiteSolrURlListDTO);

                    if (objDMSDocumentId == null)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_DMSDocumentId);
                    }
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.DMSDocumentId_Required);
                }

                General.CreateCodeLog("Step 2.10", "After validating DMSDocumentId", "", MethodBase.GetCurrentMethod().Name);
            }

            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 3.0", "Before validating DMSStatusId", "", MethodBase.GetCurrentMethod().Name);

                if (!string.IsNullOrWhiteSpace(objAPIRequest.DMSStatusId))
                {
                    objMasterBAL = new MasterBAL(_objConfigurationSettingsListDTO, strRespectiveConnectionString);
                    objDMSDMSStatusId = objMasterBAL.GetMasterDataByCodeInternalObject(SAASPOSGeneral.MasterDataCodes.DMSDocumentStatus.ToString(), objAPIRequest.DMSStatusId.Trim(), lstSiteDetails, lstSiteSolrURlListDTO);

                    if (objDMSDMSStatusId == null)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_DMSStatusId);
                    }
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.DMSStatusId_Required);
                }

                General.CreateCodeLog("Step 3.1", "After validating DMSStatusId", "", MethodBase.GetCurrentMethod().Name);
            }

            General.CreateCodeLog("Step 3.2", "After validating the Request parameters", "", MethodBase.GetCurrentMethod().Name);
            #endregion

            #region Calling DAL
            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 3.3", "Before Assigning values to the object", "", MethodBase.GetCurrentMethod().Name);

                objDMSStatusListDTO = new DMSStatusListDTO()
                {
                    DMSDocumentId = objAPIRequest.DMSDocumentId,
                    DMSStatusId = objAPIRequest.DMSStatusId,
                    UpdatedBy = strLoginUserName,
                    LastUpdated = dtTime,
                    CountryId = intCountryId,
                    CurrencyId = intCurrencyId,
                    LanguageId = intLanguageId,
                    CreatedByUserId = intUserId
                };

                General.CreateCodeLog("Step 3.4", "After Assigning values to the object", "", MethodBase.GetCurrentMethod().Name);

                try
                {
                    #region UpdateDMSStatusById

                    General.CreateCodeLog("Step 3.5", "Before Calling UpdateDMSStatusById", "", MethodBase.GetCurrentMethod().Name);

                    if (_objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(SAASPOSGeneral.DataBaseTypeId.MsSql))
                    {
                        if (strRespectiveConnectionString != null && strRespectiveConnectionString != string.Empty)
                        {
                            objDMSDAL = new DMSDAL(_objConfigurationSettingsListDTO, strRespectiveConnectionString);
                        }
                        intSuccess = objDMSDAL.UpdateDMSStatusById(objDMSStatusListDTO);
                    }
                    else if (_objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(SAASPOSGeneral.DataBaseTypeId.MySql))
                    {
                        if (strRespectiveConnectionString != null && strRespectiveConnectionString != string.Empty)
                        {
                            objMySqlDMSDAL = new MySqlDMSDAL(_objConfigurationSettingsListDTO, strRespectiveConnectionString);
                        }
                        intSuccess = objMySqlDMSDAL.UpdateDMSStatusById(objDMSStatusListDTO);
                    }                            
                    General.CreateCodeLog("Step 3.6", "After Calling UpdateDMSStatusById", "", MethodBase.GetCurrentMethod().Name);

                    if (intSuccess == 0)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Failure);
                    }

                    if (IsMicrosoftInsightsRequired)
                    {
                        var dependency = new DependencyTelemetry();
                        var success = dependency.Success.Value;
                        timer.Stop();
                        var telemetryclient = new TelemetryClient();
                        telemetryclient.TrackDependency("Redis", MethodBase.GetCurrentMethod().Name, intSuccess.ToString(), startTime, timer.Elapsed, success);
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    General.objConfigurationSettingsListDTO = _objConfigurationSettingsListDTO;
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);

                    General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                }

            }
            #endregion
        }
    }
    catch (Exception ex)
    {
        General.objConfigurationSettingsListDTO = _objConfigurationSettingsListDTO;
        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
        General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
    }
    finally
    {
        #region Error/Success response region
        try
        {
            if (ErrorCode > 0)
            {
                try
                {
                    General.CreateCodeLog("Step 3.7", "Starting of GetErrorCodeById calling - Response", "", MethodBase.GetCurrentMethod().Name);

                    objErrorCodeBAL = new ErrorCodeBAL(_objConfigurationSettingsListDTO, strRespectiveConnectionString, EncryptionKey);
                    lstErrorCodeListDTO = objErrorCodeBAL.GetErrorCodeById(_objConfigurationSettingsListDTO, ErrorCode, strRespectiveConnectionString, intLanguageId);

                    General.CreateCodeLog("Step 3.8", "Ending of GetErrorCodeById calling - Response", "", MethodBase.GetCurrentMethod().Name);
                }
                catch (Exception Ex)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                    General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                }
                finally
                {
                    objErrorCodeBAL = null;
                }

                if (lstErrorCodeListDTO != null && lstErrorCodeListDTO.Count > 0)
                {
                    objResponse = new Response<object>();
                    objResponse.ReturnCode = lstErrorCodeListDTO[0].ReturnCode;
                    objResponse.ReturnMessage = lstErrorCodeListDTO[0].ReturnMessage;
                    objResponse.Data = null;
                }
            }
            else if (ErrorCode == 0)
            {
                objResponse = new Response<object>();
                dynamic objResponsedetails = new ExpandoObject();
                objResponse.ReturnCode = 0;
                objResponse.ReturnMessage = "Success";
                objResponse.ResponseTime = Math.Round((RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes) - startResponseTime).TotalMilliseconds).ToString();
                var json = JsonConvert.DeserializeObject<dynamic>(strResponse);
                objResponse.Data = json;
            }
        }
        catch (Exception ex)
        {
            General.objConfigurationSettingsListDTO = _objConfigurationSettingsListDTO;
            ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
            General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
        }

        #endregion

        #region Inserting In APILog
        try
        {
            objAPILogDetailListDTO = new APILogDetailListDTO();
            objAPILogDetailListDTO.RequestXML = strRequest;
            objAPILogDetailListDTO.ResponseXML = JsonConvert.SerializeObject(objResponse);
            objAPILogDetailListDTO.Token = objAPIRequest.JwtToken;
            objAPILogDetailListDTO.MethodName = MethodBase.GetCurrentMethod().Name;
            objAPILogDetailListDTO.ProgramCode = _objConfigurationSettingsListDTO.ProjectName;
            objAPILogDetailListDTO.CreatedBy = LoginUserName;
            objAPILogDetailListDTO.APIMethodId = Convert.ToInt32(SAASPOSGeneral.APIMethod.UpdateDMSStatus);
            objAPILogDetailListDTO.ApiLogTypeId = _objConfigurationSettingsListDTO.APILogTypeId;

            if (objAPILogDetailListDTO != null)
            {
                Task tskInsert = Task.Run(() =>
                {
                    General.CreateCodeLog("Step 3.9", "before InsertAPILog method", "", MethodBase.GetCurrentMethod().Name);
                    long[] intAPILog = General.InsertAPILog(objAPILogDetailListDTO, strRespectiveConnectionString, _objConfigurationSettingsListDTO.EncryptionKey, _objConfigurationSettingsListDTO.LogTypeId, _objConfigurationSettingsListDTO.CodeLogRequired, lstSiteDetails, lstSiteSolrURlListDTO);
                    General.CreateCodeLog("Step 4.0", "After InsertAPILog method", "", MethodBase.GetCurrentMethod().Name);
                });
            }
        }
        catch (Exception ex)
        {
            General.objConfigurationSettingsListDTO = _objConfigurationSettingsListDTO;
            ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
            General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
        }
        #endregion

        #region Nullfying objects

        objRegularExpressions = null;
        objAPILogDetailListDTO = null;
        lstErrorCodeListDTO = null;
        objDMSDAL = null;
        objMySqlDMSDAL = null;
        #endregion
    }
    return objResponse;
}
#endregion

















#region UpdateDMSStatusById 
//*********************************************************************************************************
//Purpose            :  This API Method will be used to Update DMS Status By Id.
//Layer	             :  API
//Method Name        :	UpdateDMSStatusById
//Input Parameters   :  
//Return Values      :  
// --------------------------------------------------------------------------------------------------------
//    Version            Author                     Date               Remarks       
//  -------------------------------------------------------------------------------------------------------
//    1.0	             Jafar                      10-Oct-2024          Creation
//    1.2                Jafar                      16 Jun 2025          Added CreatedByUserId 
//*********************************************************************************************************
/// <summary>
/// This API Method will be used to Update DMS Status By Id.
/// </summary>
/// <param name="objAPIRequest"></param>
/// <returns>HttpResponseMessage</returns>
public Int64 UpdateDMSStatusById(DMSStatusListDTO objAPIRequest)
{
    Int64 intReturnValue = 0;
    using (SqlConnection connection = new SqlConnection(strConnectionString))
    {
        connection.Open();
        using (SqlCommand sqlCmd = new SqlCommand())
        {
            sqlCmd.Connection = connection;
            sqlCmd.CommandType = System.Data.CommandType.StoredProcedure;
            sqlCmd.CommandText = @"dbo.usp_DMS_UpdateDMSDocumentStatus";

            if (string.IsNullOrWhiteSpace(objAPIRequest.DMSDocumentId))
            {
                sqlCmd.Parameters.Add("@DMSDocumentGuId", SqlDbType.NVarChar).Value = System.DBNull.Value;
            }
            else
            {
                sqlCmd.Parameters.Add("@DMSDocumentGuId", SqlDbType.NVarChar).Value = objAPIRequest.DMSDocumentId;
            }
            if (string.IsNullOrWhiteSpace(objAPIRequest.DMSStatusId))
            {
                sqlCmd.Parameters.Add("@DMSDocumentStatusGuId", SqlDbType.NVarChar).Value = System.DBNull.Value;
            }
            else
            {
                sqlCmd.Parameters.Add("@DMSDocumentStatusGuId", SqlDbType.NVarChar).Value = objAPIRequest.DMSStatusId;
            }
            if (objAPIRequest.CountryId > 0)
            {
                sqlCmd.Parameters.Add("@CountryId", SqlDbType.Int).Value = objAPIRequest.CountryId;
            }
            else
            {
                sqlCmd.Parameters.Add("@CountryId", SqlDbType.Int).Value = System.DBNull.Value;
            }

            if (objAPIRequest.CurrencyId > 0)
            {
                sqlCmd.Parameters.Add("@CurrencyId", SqlDbType.Int).Value = objAPIRequest.CurrencyId;
            }
            else
            {
                sqlCmd.Parameters.Add("@CurrencyId", SqlDbType.Int).Value = System.DBNull.Value;
            }

            if (objAPIRequest.LanguageId > 0)
            {
                sqlCmd.Parameters.Add("@LanguageId", SqlDbType.Int).Value = objAPIRequest.LanguageId;
            }
            else
            {
                sqlCmd.Parameters.Add("@LanguageId", SqlDbType.Int).Value = System.DBNull.Value;
            }
            sqlCmd.Parameters.Add("@UpdatedBy", SqlDbType.NVarChar).Value = objAPIRequest.UpdatedBy;
            sqlCmd.Parameters.Add("@LastUpdated", SqlDbType.DateTime).Value = objAPIRequest.LastUpdated;

            if (objAPIRequest.CreatedByUserId > 0)
            {
                sqlCmd.Parameters.Add("CreatedByUserId", SqlDbType.BigInt).Value = objAPIRequest.CreatedByUserId;
            }
            else
            {
                sqlCmd.Parameters.Add("CreatedByUserId", SqlDbType.BigInt).Value = System.DBNull.Value;
            }

            object objReturnValue = sqlCmd.ExecuteScalar();

            if (objReturnValue != null)
            {
                Int64.TryParse(objReturnValue.ToString(), out intReturnValue);
            }
            return intReturnValue;
        }
    }
}
#endregion




















using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Revalsys.Common;
using Revalsys.DMS.BAL;
using Revalsys.DMS.RevalProperties;
using Revalsys.Properties;
using Revalsys.Security;
using System;
using System.Linq;
using System.Reflection;
namespace RevalsysSAASPOSWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [JwtAuthentication]
    public class UpdateDMSStatusByIdController : ControllerBase
    {
        private ConfigurationSettingsListDTO _ConfigurationSettingsListDTO = null;

        public UpdateDMSStatusByIdController(IOptions<ConfigurationSettingsListDTO> options)
        {
            _ConfigurationSettingsListDTO = options.Value;
        }

        #region UpdateDMSStatusById
        //*********************************************************************************************************
        //Purpose            :  This API is Used to Update the DMS Status By Id.
        //Layer	             :  API
        //Method Name        :	UpdateDMSStatusById
        //Input Parameters   :  
        //Return Values      :  
        // --------------------------------------------------------------------------------------------------------
        //    Version              Author                     Date                  Remarks       
        //  -------------------------------------------------------------------------------------------------------
        //    1.0	             Jafar Sadik               10 Oct 2024              Creation
        //*********************************************************************************************************
        /// <summary>
        /// This API is Used to Update the DMS Status By Id.
        /// </summary>
        /// <param name="objLoginRequest"></param>
        /// <returns>HttpResponseMessage</returns>
        [HttpPost]
        [Authorize]
        public ContentResult Post(DMSStatusRequestListDTO objDMSStatusRequestListDTO)
        {
            var HeaderType = Request.ContentType;
            DMSBAL objDMSBAL = null;
            ContentResult objContentResult = null;
            InvalidRequest objInvalidRequest = new InvalidRequest();
            objInvalidRequest.ReturnCode = CommonResponse.CommonResponseErrorCodes.InvalidRequest.ToString();
            objInvalidRequest.ReturnMessage = CommonResponse.dictCommonResponse[CommonResponse.CommonResponseErrorCodes.InvalidRequest.ToString()];
            Int32 StatusCode = 0;
            Response<object> objResponse = null;
            object objResult = null;
            string strToken = string.Empty;
            string LogTypeId = string.Empty;
            string JWTToken = string.Empty;

            var strClaimsToken = HttpContext.User.Claims;
            #region After validating request

            try
            {
                if (_ConfigurationSettingsListDTO != null && objDMSStatusRequestListDTO != null)
                {
                    General.objConfigurationSettingsListDTO = _ConfigurationSettingsListDTO;
                    var strTokenDetails = strClaimsToken.ToList();
                    if (strTokenDetails != null && strTokenDetails.Count > 0)
                    {
                        objDMSStatusRequestListDTO.JwtToken = strTokenDetails.Where(c => c.Type == "stk").Select(c => c.Value).SingleOrDefault();
                        objDMSStatusRequestListDTO.upk = strTokenDetails.Where(c => c.Type == "upk").Select(c => c.Value).SingleOrDefault();
                    }
                    String TokenExpiryTime = string.Empty;
                    string UserPrivateKey = string.Empty;
                    General.CreateCodeLog("Step 1", "Starting of BAL calling - Request", objDMSStatusRequestListDTO, MethodBase.GetCurrentMethod().Name);
                    objDMSBAL = new DMSBAL(_ConfigurationSettingsListDTO);
                    objResponse = objDMSBAL.UpdateDMSStatusById(objDMSStatusRequestListDTO);
                    General.CreateCodeLog("Step 2", "After of BAL calling - objResponse : ", objResponse, MethodBase.GetCurrentMethod().Name);
                    if (objResponse != null)
                    {
                        objResult = objResponse;
                    }
                    else
                    {
                        objResult = objResponse;
                    }
                }
                else
                {
                    objResult = objInvalidRequest;
                }

                StatusCode = (int)CommonResponse.CommonResponseErrorCodes.Success;
            }
            catch (Exception ex)
            {
                General.objConfigurationSettingsListDTO = _ConfigurationSettingsListDTO;

                //General.CreateErrorLog(ex);
                General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name);

                StatusCode = (int)CommonResponse.CommonResponseErrorCodes.InvalidRequest;
            }
            finally
            {
                objDMSBAL = null;
            }

            #endregion

            #region output converting xml or json
            if (HeaderType != null)
            {
                if (HeaderType.ToString().ToLower().Contains("application/xml")) //converting the xml
                {
                    objContentResult = new ContentResult() { Content = clsSecurity.ConvertObjectToXml(objResult), ContentType = "application/xml", StatusCode = StatusCode };
                }
                else if (HeaderType.ToString().ToLower().Contains("application/json"))
                {
                    objContentResult = new ContentResult() { Content = JsonConvert.SerializeObject(objResult), ContentType = "application/json", StatusCode = StatusCode };
                }
                else
                {
                    objContentResult = new ContentResult() { Content = JsonConvert.SerializeObject(objResult), ContentType = "application/json", StatusCode = StatusCode };
                }
            }
            else
            {
                objContentResult = new ContentResult() { Content = JsonConvert.SerializeObject(objResult), ContentType = "application/json", StatusCode = StatusCode };
            }
            return objContentResult;
            #endregion
        }
        #endregion
    }
}

























        public enum MasterDataCodes
        {
            TimeZone = 1,
            Language = 2,
            ProductType = 3,
            UOMType = 4,
            TaxType = 5,
            DiscountType = 6,
            TaxRate = 7,
            CashRegister = 8,
            SiteCategory = 9,
            Category = 10,
            Variant = 11,
            Store = 12,
            Gender = 13,
            Role = 14,
            PaymentType = 15,
            OrderStatus = 16,
            State = 17,
            OTPType = 18,
            Module = 19,

            LengthType = 21,
            BloodGroup = 22,
            EmployeeStatus = 23,
            MaritalStatus = 24,
            IMAddressType = 26,
            AdvanceType = 27,
            PolicyType = 28,
            Solr = 29,
            FileUploadType = 30,
            FileUploadStatus = 31,
            Designation = 32,         // Added by Akhila R on 27 Nov 2023
            Department = 33,
            Country = 34,
            City = 35,
            CRMIndustry = 36,
            CRMLeadSource = 37,
            CRMDealStatus = 38,
            Customer = 41,
            CRMLead = 43,  // Added by Akhila R on 06 Dec 2023
            CRMModeOfActivity = 44,

            IMItem = 49, // Added by AKhila M on 30 oct 2023
            IMVendor = 50,
            IMAgent = 47,
            PurchaseOrderStatus = 51,
            IMCourier = 48,
            IMAddress = 46,
            Permission = 1058,
            IMAttribute = 52,
            SocialChannel = 42,
            ReasonType = 39,
            RequestType = 40,
            LeaveRequestType = 45,
            HRMSEmployee = 1063,
            EmployeeGrievanceStatusType = 1064,

            IMItemCategory = 53, //Added by Rajesh K on 14 Dec 2023
            IMPurchaseOrderDetail = 1055, //Added by Rajesh K on 14 Dec 2023
            IMInVoice = 1053, //Added by Rajesh K on 14 Dec 2023
            IMInVoiceDetail = 1054, //Added by Rajesh K on 14 Dec 2023
            IMPurpose = 1056,
            IMItemBarcode = 1057,



            ReturnPaymentType = 1061,
            ShareType = 1062,
            ReturnReason = 1068,

            PrintType = 1065,
            HRMSHealthInsurance = 1066,
            Relationship = 1067,
            LeaveStatus = 1059,       //Added by Hanumanthu S on 30 Jan 2024
            LeaveType = 1069,            //Added by Hanumanthu S on 30 Jan 2024

            CRMDealStage = 1075, //Added by Madhuri P. on 16 Feb 2024
            CRMDealStageReason = 1074,  //Added by Madhuri P. on 16 Feb 2024
            CRMDeal = 1070,
            User = 1071,
            CRMLeadStatus = 1072,
            EmployeeAdditionalWorkStatus = 1073,  //Added by Upendra g on 05 Mar 2024
            HRMSSalaryComponent = 1077,
            HRMSSalaryTemplate = 1078,
            Employee_Additional_Work = 1080,
            HRMSEmployeeSalary = 1081,
            UserStore = 1082, //added by anusha
            StoreList = 1114,
            ProductVarient = 1115,
            SaleStore = 1116,
            HRMSTravelClass = 1086, // Added by Amarjeet on 20 Apr 2024
            HRMSSpendMode = 1087, // Added by Amarjeet on 20 Apr 2024
            HRMSEmployeeExpenseStatus = 1088, // Added by Amarjeet on 20 Apr 2024
            HRMSTravelMode = 1083, // Added by Amarjeet on 20 Apr 2024
            HRMSAssociatedTravelRequest = 1084,  // Added by Amarjeet on 20 Apr 2024
            HRMSExpenseType = 1085,   // Added by Amarjeet on 20 Apr 2024
            HRMSSalaryComponentType = 1109,

            LoyaltyOfferType = 1089,
            LoyaltyOfferMode = 1090,
            LoyaltyOfferTransactionType = 1091,
            LoyaltyOfferApplicable = 1092,
            LoyaltyRuleType = 1093,
            LoyaltyAmountType = 1094,
            LoyaltyDurationType = 1095,
            LoyaltyDataLinkUpType = 1096,
            LoyaltyAssociation = 1097,
            LoyaltyTierPolicy = 1098,
            LoyaltyLapsePolicy = 1099,
            HRMSTask = 1104,           // Added by Upendra G on 28 May 2024
            HRMSTaskStatus = 1105,     // Added by Upendra G on 28 May 2024
            HRMSTaskType = 1106,       // Added by Upendra G on 28 May 2024
            HRMSClientProject = 1107,  // Added by Upendra G on 28 May 2024
            HRMSLeave = 1108,
            LoyaltyOffer = 1110,
            LoyaltyOfferRule = 1111,
            HRMSEmployeeTask = 1117,
            HRMSEmployeeClientProject = 1118,
            SupportTicketCategory = 1120, // Added by Pavan K on 13 Jun 2024
            SupportTicketPriority = 1121, // Added by Pavan K on 13 Jun 2024
            SupportIssueType = 1122, // Added by Pavan K on 13 Jun 2024
            SupportTicketTemplate = 1123, //Added by Pavan K on 13 Jun 2024
            SupportTicketSource = 1124, // Added by Pavan K on 13 Jun 2024
            SupportChannelOfPurchase = 1125, //Added by Pavan K on 13 Jun 2024
            SupportTagWord = 1126,  //Added by Pavan K on 13 Jun 2024
            SupportTicketStatus = 1127, //Added by Pavan K on 13 Jun 2024
            SupportTicketActionType = 1128, //Added by Pavan K on 14 Jun 2024
            ManagerHRMSEmployee = 1129, //Added by Nikitha N on 26 Jun 2024
            HrHRMSEmployee = 1130, //Added by Nikitha N on 26 Jun 2024
            ReviewStatus = 1131, //Added by Nikitha N on 26 jun 2024
            Subscription = 1132,
            ProductVariantCode = 1133, //Added by Nikitha N on 16 jun 2024
            BarcodeGenerationType = 1134, //Added by Nikitha N on 16 jun 2024
            Product = 1135, // Added by Nikitha N on 19 jun 2024
            ProductVariantDetail = 1136, //Added by Nikitha N on 19 jun 2024
            SupportTicketInstance = 1137, // Added by Mahesh Gupta 19 Jul 2024
            SupportTicketBrowser = 1138,   // Added by Mahesh Gupta 19 Jul 2024
            SubscriptionModuleCode = 1140, // Added by manikanta k 19/july/2024
            Site = 1141,  // Added by manikanta k 19/july/2024
            HRMSModule = 1142, //Added By Anil 23/july /2024
            HRMSDocumentCategoryTask = 1143, //Added By Anil 23/july /2024
            FulfillmentStatus = 1144, //Added by Ajit Patil on 29 Jul 2024
            SupportTicketModule = 1145, // Added by Mahesh Gupta 24 July 2024
            SupportTicketProject = 1146, // Added by Mahesh Gupta 24 July 2024
            SupportTicketFormField = 1147, // Added by Mahesh Gupta 24 July 2024
            ParentTaskNumber = 1148, //Added By Anil 23/july /2024
            FinanceLedger = 1149,
            District = 1150,  // Added By Mahesh Gupta 13 Aug 2024
            ComplaintDepartment = 1151, // Added By Mahesh Gupta 13 Aug 2024
            GetRole = 1152,
            ComplaintStatus = 1153,
            FinanceTaxType = 1154,
            FinanceGSTType = 1155,
            JournalStatus = 54,
            FinanceVendorAccount = 55,
            FinanceVendorGSTType = 56,
            FinanceGSTTypeLedger = 57,
            FinanceTaxTypeLedger = 1160,
            FinanceTransactionType = 1161,
            HRMSEmployeeAssessment = 1162,
            DMSDocumentStatus = 1169,
            HRMSLeaveCycle = 1165,
            HRMSLeaveExpiryPolicy = 1166,
            DMSDocumentVersion = 1164,
            AlertAssign = 1167,              // Added By Debabrata Meher on 28 Oct 2024
            SubSupportTicketCategory = 1168,  // Added By Debabrata Meher on 28 Oct 2024
            BankName = 1170,
            financePaymentMethod = 1171,
            FinanceJournal = 1172,
            FinanceTransactionCode = 1173,
            FinanceJournalDetail = 1174,
            HealthHospital = 1159,  // Added By Fayaz on 29 Oct 2024
            HealthDoctorTimeSlotType = 1158,  // Added By Fayaz on 29 Oct 2024
            HealthAccessType = 1156,  // Added By Fayaz on 29 Oct 2024
            HealthDoctorTimeSlot = 1157, // Added By Fayaz on 07 Nov 2024
            HealthAppointmentStatus = 58, // Added By Fayaz on 13 Nov 2024
            HealthLicenseExpiryDuration = 1185,  //Added by Akhil N on 2 Nov 2024
            HealthLicenseStatus = 1186, //Added by Akhil N on 2 Nov 2024
            HealthApprovalStatus = 1189, //Added By Nikitha N on 13 Nov 2024
            HRMSPermissionHour = 1187,   // Added by charan on 18 Nov 2024      
            LeaveAccuralPeriodType = 1190, // Mahesh Gupta 20 Nov 2024
            IMPackageType = 1191, //Added Nikitha N 25 Nov 2024
            IMManufacturer = 1192, //Added Nikitha N 25 Nov 2024
            VoucherType = 59,
            IMPurchaseOrder = 1194,
            Location = 1198,      //Added  By Nikitha N  on 13 Dec 2024 
            HRMSEmployeeCompensatoryLeaveStatus = 1199,      //Added   on 27 Dec 2024 
            HealthSpeciality = 1200, // Added By Fayaz on 06 Jan 2025
            HRMSClient = 1201,
            Year = 1195,
            Month = 1196,
            HRMSFollowUpStatus = 1202,   //Added Anil  02 Dec 2024
            HealthPrescriptionFrequency = 1203,
            DMSDocument = 1204,
            HRMSMonthlyAttendanceStatus = 1205,
            LogType = 1206,       //Added By Manikanta  on 05 Dec 2024
            HealthAdministrationMethod = 1163,
            HealthMedicationFrequency = 1181,
            FinanceSection = 1207,
            FinanceTaxTypeDetail = 1208,
            EmployeeUser = 1224,
            RDLCFileType = 1209,
            RDLCMandatory = 1210,
            RDLCInputType = 1211,
            RDLCAcceptanceCriteria = 1212,
            RDLCDevelopmentType = 1213,
            RDLCDevelopmentLanguage = 1214,
            RDLCFramework = 1215,
            RDLCVersion = 1216,
            RDLCCodingStandard = 1217,
            RDLCModule = 1218,
            RDLCBusinessComponent = 1219,
            RDLCBRD = 1220,
            RDLCType = 1221,
            RDLCHttpMethod = 1222,
            WorkFlowStatus = 1223,
            RDLCContentBox = 1225,
            Modules = 1226,            // Added By Manikanta K
            IOTType = 1230,     //Add By Muni B
            IOTProvider = 1229,     //Add By Muni B
            IOTSourceColumn = 1231,      //Add By Muni B           
            HRMSEnrollmentStatus = 1232,     //Added by Nikitha N on 01 Apr 2025
            JobSeekerStatus = 1234,       //Added by Abhijit S
            HRMSSalaryHoldType = 1235,   // Added by Akhila R on 02 May 2025 
            HRMSEmployeeSalaryHoldRemark = 1238,   // Added by Akhila R on 06 May 2025 
            IOTAlertSeverityType = 1241,
            IOTState = 1242,
            IOTDistrict = 1243,
            IOTSubDistrict = 1244,
            HRMSEmployeeSalaryHold = 1240,
            FinanceApprovalUser = 1247,
            FinancialYearClosureSteps = 22445,
            FaceAuthProvider = 22447, // Added By Pavan K on 18 Jul 2025 use in main DB
            IOTUploadTemplate = 22448,
            Client = 22449,
            CommunicationServiceType = 22452,
            AlertType = 1060,
            MainBackGroundService = 1061,
            IOTTypeParameter = 2245,
            HRMSProjectTask = 2246,
            AssetManufacturer = 22467,
            AssetCategory = 22468,
            AssetSubCategory = 90,
            Asset = 22456,
            RFQStatus = 22458,                    // Added By Charan On 14 Jul 2025
            RFQDocumentType = 22462,             // Added By Charan On 14 Jul 2025
            RFQTab = 22469,                       // Added By Charan On 15 Jul 2025
            RFQPaymentTerm = 22464,                  // Added By Charan On 15 Jul 2025
            RFQPricingType = 22461,              // Added By Charan On 15 Jul 2025
            RFQCurrency = 22730,                    // Added By Charan On 15 Jul 2025
            RFQDeliveryIncoTerm = 22463,             // Added By Charan On 15 Jul 2025
            AlertTriggerCondition = 22470,      // Added By Chityala Vinod On 16 Jul 2025
            AlertNotificationType = 22471,      // Added By Chityala Vinod On 16 Jul 2025
            SMSTemplate = 22473,                // Added By Chityala Vinod On 16 Jul 2025
            StoreEmailTemplate = 22474,         // Added By Chityala Vinod On 16 Jul 2025
            WhatsAppTemplate = 22476,           // Added By Chityala Vinod On 16 Jul 2025
            AlertField = 22477,                 // Added By Chityala Vinod On 16 Jul 2025
            AlertOperator = 22478,              // Added By Chityala Vinod On 16 Jul 2025
            RFQHRMSEmployee = 22479,             // Added By Charan On 17 Jul 2025
            CommunicationChannelType = 22480,      // Added By Chityala Vinod On 17 Jul 2025
            NotificationTemplate = 22455,           // Added By Chityala Vinod On 17 Jul 2025
            IVRTemplate = 22481,                   // Added By Chityala Vinod On 17 Jul 2025
            RFQQuoteDecision = 22485,
            RFQQuoteStatus = 22459,
            GSTLedger = 22486,
            ProductClassification = 22482,           // Added by Manisha B on July 2025
            HRMSWeekOff = 22483, //Added by subbarao B on aug 01 2025
            HRMSTaskProgress = 22490,
            HRMSEmployeeMeetingAttendance = 22488,
            HRMSScrumTemplate = 22489,
            DefaultStore = 22484,
            RoleType = 22500,
            BedType = 22501,
            Candidate = 22556,    // Added by Akhila R on 03 Sep 2025
            HRMSCVSource = 1236,    // Added by Akhila R on 04 Sep 2025
            HRMSJDStatus = 22557,
            HRMSRemark = 22589,
            SocialProvider = 22590,
            ProductImageType = 22706,      // Added By Mahesh Gupta
            Currency = 22465,
            Title = 22502,
            ProductCancellationChargesType = 22503,
            ProductRelation = 22504,
            AuthenticateUserType = 22740,   // Added by Musthafa M on 17 Sep 2027
            HRMSConsultancy = 22546,   // Added by Akhila R on 16 Sep 2025
            HRMSRaiseRequisition = 22535,   // Added by Akhila R on 16 Sep 2025
            FeedBackRounds = 22695,   // Added by Akhila R on 16 Sep 2025
            FeedBackQuestion = 22705,    // Added by Akhila R on 16 Sep 2025
            FeedBackQuestionHeader = 22696,    // Added by Akhila R on 16 Sep 2025
            FeedbackQuestionResult = 22727,  // Added by Akhila R on 16 Sep 2025
            FeedbackQuestionRound = 22715,    // Added by Akhila R on 16 Sep 2025
            QuestionChoice = 22716,    // Added by Akhila R on 16 Sep 2025
            HRMSCandidateAssessment = 22735,   // Added by Akhila R on 16 Sep 2025
            HRMSFeedbackRecommendation = 22741,   // Added by Akhila R on 17 Sep 2025
            POSTax = 22742,
            ProductTaxRate = 22743,
            POSPaymentGateway = 22744,
            Order = 22745,
            OrderByOrderStatus = 22746,
            VerifyOrderNumber = 22747,
            TravellerAddress = 22766,
            DefaultBank = 23896, // Added by Abhijit S on 13 Oct 2025
            OrderDetail = 22767,
            HRMSOnBoardDocumentUploadStatus = 23965, //Added by Akhila R on 15 Oct 2025
            HRMSCandidateOnBoardStatus = 23973, // Added by Akhila R on 15 Oct 2025
            CandidateSubmission = 22768,
            VideoType = 23991,
            CouponType = 22768, // Added by Subbarao B on 16 Oct 2025
            CouponCategoryType = 22769,// Added by Subbarao B on 16 Oct 2025
            CouponApplyOn = 22770,// Added by Subbarao B on 16 Oct 2025
            CouponDiscountType = 22771,// Added by Subbarao B on 16 Oct 2025
            CouponCondition = 22772,// Added by Subbarao B on 16 Oct 2025
            StorePaymentGroup= 22773, // Added by Ajit  on 24 Nov 2025
            Domain = 25417, // Added by Prakash T
            DashBoardMap = 25419, // Added by Prakash T
            Application = 25451, // Added By Mahesh Gupta 12 Feb 2026
            EmployeeByUserId = 25452, 
            HTTPStatusMethodId = 25402,
            ContentTypeId = 25404,
            HRMSShiftManagement = 25460,   // Added By Manikanta K 03 Apr 2026
            HRMSWorkLocation = 22521,     // Added By Manikanta K 03 Apr 2026
            HRMSLeaveTemplate = 24163,      // Added By Manikanta K 03 Apr 2026
            RelationShip = 1067,          // Added By Manikanta K 03 Apr 2026
            HRMSProbationStatus = 25408,  // Added By Manikanta K 03 Apr 2026
            HRMSTechnology = 24175,          // Added By Manikanta K 03 Apr 2026
            HRMSNoticePeriod = 24168,      // Added By Manikanta K 03 Apr 2026
            HRMSEmployeeLeaveType = 25411,  // Added By Manikanta K 03 Apr 2026
            ASSETAllocationStatus = 25412,  // Added By Manikanta K 03 Apr 2026
            AssetLocationStatus = 25413,    // Added By Manikanta K 03 Apr 2026
            Branch = 25414,              // Added By Manikanta K 03 Apr 2026
            IdProofType = 22544,         // Added By Manikanta K 03 Apr 2026
            AssetVendor = 25415,         // Added By Manikanta K 03 Apr 2026
            AssetStatus = 25416,        // Added By Manikanta K 03 Apr 2026
            HealthEmployeeType  = 24064,    // Added By Manikanta K 03 Apr 2026
            HRMSDocumentCategory  = 26556   // Added By Manikanta K 03 Apr 2026   

        }





using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Revalsys.Common;
using Revalsys.Properties;
using Revalsys.SearchData.BusinessLogic;
using Revalsys.DynamicForm.RevalProperties;
using Revalsys.Security;
using System.Reflection;

namespace RevalsysSAASPOSWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [JwtAuthentication]
    public class GetStaticDataByCodeController : ControllerBase
    {
        private ConfigurationSettingsListDTO _ConfigurationSettingsListDTO = null;
        public GetStaticDataByCodeController(IOptions<ConfigurationSettingsListDTO> options)
        {
            _ConfigurationSettingsListDTO = options.Value;
        }

        #region GetDataByCode
        //*********************************************************************************************************
        //Purpose            :  This API Method will Get details by module code .
        //Layer	             :  API
        //Method Name        :	GetDataByCode
        //Input Parameters   :  
        //Return Values      :  
        // --------------------------------------------------------------------------------------------------------
        //    Version            Author                     Date               Remarks       
        //  -------------------------------------------------------------------------------------------------------
        //    1.0	            Nagaraju k                 09 Oct 2025        Creation
        //*********************************************************************************************************
        /// <summary>
        /// This API Method will Get details by module code.
        /// </summary>
        /// <param name=""></param>
        /// <returns>ContentResult</returns>
        /// 
        [HttpPost]
        public async Task<ContentResult> GetDataByCode(SearchCodeListDTO objAPIRequest)
        {
            var HeaderType = Request.ContentType;
            SearchDataBAL objSearchDataBAL = null;
            ContentResult objContentResult = null;
            Response<object> objDataBySearch = null;
            object objResult = null;
            List<string> lstTableNames = null;
            InvalidRequest objInvalidRequest = new InvalidRequest();
            objInvalidRequest.ReturnCode = ((int)CommonResponse.CommonResponseErrorCodes.InvalidRequest).ToString();
            objInvalidRequest.ReturnMessage = Enum.GetName(typeof(CommonResponse.CommonResponseErrorCodes), CommonResponse.CommonResponseErrorCodes.InvalidRequest);
            Int32 StatusCode = 0;
            var strClaimsToken = HttpContext.User.Claims;
     

            #region After validating request
            try
            {
                if (_ConfigurationSettingsListDTO != null)
                {
                    var strTokenDetails = strClaimsToken.ToList();
                    if (strTokenDetails != null && strTokenDetails.Count > 0)
                    {
                        objAPIRequest.JwtToken = strTokenDetails.Where(c => c.Type == "stk").Select(c => c.Value).SingleOrDefault();
                        objAPIRequest.upk = strTokenDetails.Where(c => c.Type == "upk").Select(c => c.Value).SingleOrDefault();
                        objAPIRequest.cpk = strTokenDetails.Where(c => c.Type == "cpk").Select(c => c.Value).SingleOrDefault();
                        objAPIRequest.IsStaticApiTemplate = true;
                    }

                    #region Declaring TableNames List
                    string[] TableNames = { "Details" };
                    #endregion

                    General.objConfigurationSettingsListDTO = _ConfigurationSettingsListDTO;

                    Task<Response<object>> tskResponse = Task<Response<object>>.Run(() =>
                    {
                        General.CreateCodeLog("Step 1", "Starting of GetDataByCode BAL calling - Response", objAPIRequest);
                        objSearchDataBAL = new SearchDataBAL(_ConfigurationSettingsListDTO);
                        objDataBySearch = objSearchDataBAL.GetDataByCode(objAPIRequest, TableNames, lstTableNames);
                        General.CreateCodeLog("Step 2", "Ending of GetDataByCode BAL calling - Response", objDataBySearch);
                        return objDataBySearch;
                    });
                    objDataBySearch = await tskResponse;

                    if (objDataBySearch != null)
                    {
                        objResult = objDataBySearch;
                    }
                    else
                    {
                        objResult = objInvalidRequest;
                    }
                }
                else
                {
                    objResult = objInvalidRequest;
                }
                StatusCode = (int)CommonResponse.CommonResponseErrorCodes.Success;
            }
            catch (Exception ex)
            {
                if (_ConfigurationSettingsListDTO != null)
                {
                    General.objConfigurationSettingsListDTO = _ConfigurationSettingsListDTO;
                    General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name);
                }
                StatusCode = (int)CommonResponse.CommonResponseErrorCodes.BadRequest;
            }
            finally
            {
                #region Nullifying Objects
                objDataBySearch = null;
                objInvalidRequest = null;
                #endregion
            }
            #endregion

            #region output converting xml or json
            if (HeaderType != null)
            {
                if (HeaderType.ToString().ToLower().Contains("application/xml")) //converting the xml
                {
                    objContentResult = new ContentResult() { Content = clsSecurity.ConvertObjectToXml(objResult), ContentType = "application/xml", StatusCode = StatusCode };
                }
                else if (HeaderType.ToString().ToLower().Contains("application/json"))
                {
                    objContentResult = new ContentResult() { Content = JsonConvert.SerializeObject(objResult), ContentType = "application/json", StatusCode = StatusCode };
                }
                else
                {
                    objContentResult = new ContentResult() { Content = JsonConvert.SerializeObject(objResult), ContentType = "application/json", StatusCode = StatusCode };
                }
            }
            else
            {
                objContentResult = new ContentResult() { Content = JsonConvert.SerializeObject(objResult), ContentType = "application/json", StatusCode = StatusCode };
            }
            return objContentResult;
            #endregion
        }
        #endregion
    }
}



using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Revalsys.Common;
using Revalsys.Properties;
using Revalsys.SearchData.BusinessLogic;
using Revalsys.DynamicForm.RevalProperties;
using Revalsys.Security;
using System.Reflection;

namespace RevalsysSAASPOSWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [JwtAuthentication]
    public class GetStaticDataBySearchController : ControllerBase
    {
        private ConfigurationSettingsListDTO _ConfigurationSettingsListDTO = null;

        public GetStaticDataBySearchController(IOptions<ConfigurationSettingsListDTO> options)
        {
            _ConfigurationSettingsListDTO = options.Value;
        }

        #region GetStaticDataBySearch
        //*********************************************************************************************************
        //Purpose            :  This API Method will Get Data By Search.
        //Layer	             :  API
        //Method Name        :	GetStaticDataBySearch
        //Input Parameters   :  
        //Return Values      :  
        // --------------------------------------------------------------------------------------------------------
        //    Version            Author                     Date               Remarks       
        //  -------------------------------------------------------------------------------------------------------
        //    1.0	            Nagaraju k                 09 Oct 2025         Creation
        //*********************************************************************************************************
        /// <summary>
        /// This API Method will Get Static Data By Search.
        /// </summary>
        /// <param name=""></param>
        /// <returns>ContentResult</returns>
        /// 
        [HttpPost]
        public async Task<ContentResult> GetDataBySearch(SearchDataListDTO objAPIRequest)
        {
            var HeaderType = Request.ContentType;
            SearchDataBAL objSearchDataBAL = null;
            ContentResult objContentResult = null;
            Response<object> objDataBySearch = null;
            object objResult = null;
            InvalidRequest objInvalidRequest = new InvalidRequest();
            objInvalidRequest.ReturnCode = ((int)CommonResponse.CommonResponseErrorCodes.InvalidRequest).ToString();
            objInvalidRequest.ReturnMessage = Enum.GetName(typeof(CommonResponse.CommonResponseErrorCodes), CommonResponse.CommonResponseErrorCodes.InvalidRequest);
            Int32 StatusCode = 0;
            var strClaimsToken = HttpContext.User.Claims;

            #region After validating request
            try
            {
                if (_ConfigurationSettingsListDTO != null)
                {
                    var strTokenDetails = strClaimsToken.ToList();
                    if (strTokenDetails != null && strTokenDetails.Count > 0)
                    {
                        objAPIRequest.JwtToken = strTokenDetails.Where(c => c.Type == "stk").Select(c => c.Value).SingleOrDefault(); // strTokenDetails[1].Value.ToString();
                        objAPIRequest.upk = strTokenDetails.Where(c => c.Type == "upk").Select(c => c.Value).SingleOrDefault();
                        objAPIRequest.cpk = strTokenDetails.Where(c => c.Type == "cpk").Select(c => c.Value).SingleOrDefault();
                        objAPIRequest.IsStaticApiTemplate = true;
                    }

                    General.objConfigurationSettingsListDTO = _ConfigurationSettingsListDTO;

                    Task<Response<object>> tskResponse = Task<Response<object>>.Run(() =>
                    {
                        General.CreateCodeLog("Step 1", "Starting of GetDataBySearch BAL calling - Response", objDataBySearch);
                        objSearchDataBAL = new SearchDataBAL(_ConfigurationSettingsListDTO);
                        objDataBySearch = objSearchDataBAL.GetDataBySearch(objAPIRequest);
                        General.CreateCodeLog("Step 2", "Ending of GetDataBySearch BAL calling - Response", objDataBySearch);
                        return objDataBySearch;
                    });
                    objDataBySearch = await tskResponse;

                    if (objDataBySearch != null)
                    {
                        objResult = objDataBySearch;
                    }
                    else
                    {
                        objResult = objInvalidRequest;
                    }
                }
                else
                {
                    objResult = objInvalidRequest;
                }
                StatusCode = (int)CommonResponse.CommonResponseErrorCodes.Success;
            }
            catch (Exception ex)
            {
                if (_ConfigurationSettingsListDTO != null)
                {
                    General.objConfigurationSettingsListDTO = _ConfigurationSettingsListDTO;
                    General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name);

                }
                StatusCode = (int)CommonResponse.CommonResponseErrorCodes.BadRequest;
            }
            finally
            {
                #region Nullifying Objects
                objDataBySearch = null;
                objInvalidRequest = null;
                #endregion
            }
            #endregion

            #region output converting xml or json
            if (HeaderType != null)
            {
                if (HeaderType.ToString().ToLower().Contains("application/xml")) //converting the xml
                {
                    objContentResult = new ContentResult() { Content = clsSecurity.ConvertObjectToXml(objResult), ContentType = "application/xml", StatusCode = StatusCode };
                }
                else if (HeaderType.ToString().ToLower().Contains("application/json"))
                {
                    objContentResult = new ContentResult() { Content = JsonConvert.SerializeObject(objResult), ContentType = "application/json", StatusCode = StatusCode };
                }
                else
                {
                    objContentResult = new ContentResult() { Content = JsonConvert.SerializeObject(objResult), ContentType = "application/json", StatusCode = StatusCode };
                }
            }
            else
            {
                objContentResult = new ContentResult() { Content = JsonConvert.SerializeObject(objResult), ContentType = "application/json", StatusCode = StatusCode };
            }
            return objContentResult;
            #endregion
        }
        #endregion
    }
}











#region GetDataBySearch
//*********************************************************************************************************
//Purpose            :  This API Method will get the Get Data By Search.
//Layer	             :  API
//Method Name        :	GetDataBySearch
//Input Parameters   :  
//Return Values      :  
// --------------------------------------------------------------------------------------------------------
//    Version            Author                     Date               Remarks       
//  -------------------------------------------------------------------------------------------------------
//    1.0	            Upendra G                 04 Aug 2023        Creation
//    1.1               Amarjeet                  13 Feb 2023         Added Validation for Code1 and Code2
//                                                                    for a Specific ModuleId.
//    1.2               Prakash T                 03 Oct 2025          RDLC - REV0036/MS/RDLC/865 -Common dll separation changes - Revalsys.SAASPOSCommon Reference Added
//                                                                     and using SAASPOSGeneral for enums and using SAASPOSRegularExpressions.cs for Regex validation
//    1.3               Abhijit S                 07 Now 2025         Added Condition For Getting only IsPublished records in the response when IsCheckAuthenticate is false
//*********************************************************************************************************
/// <summary>
/// This API Method will get the Get Data By Search.
/// </summary>
/// <param name=""></param>
/// <returns>ContentResult</returns>
/// 
public Response<object> GetDataBySearch(SearchDataListDTO objAPIRequest)
{
    #region Common Variables
    DateTime startResponseTime = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes);
    int ErrorCode = 0;
    bool IsCustomer = false;
    APILogDetailListDTO objAPILogDetailListDTO = null;
    List<SiteDetails> lstSiteDetails = null;
    string strRespectiveConnectionString = string.Empty;
    string strSiteToken = string.Empty;
    DateTime dtTokenExpiryTime = DateTime.MinValue, dtExpiryTime = DateTime.MinValue;
    string strUserPrivateKey = string.Empty;
    List<SiteUserListDTO> lstSiteUserDetailByPrivateKey = null;
    #endregion

    #region Specific Variables
    Response<object> objResponse = null;
    CommonSearchListDTO objSearchListDTO = null;
    List<SearchDataListDTO> lstSearchDataListDTO = null;
    SAASPOSRegularExpressions objRegularExpressions = null;
    SearchDataDAL objSearchDataDAL = null;
    bool IsActive = false, IsAuthorizedUser = false, ESerachWordRequird = false, IsFromMegaMenu = false, IsAdminView = false;
    TimeZone = "India Standard Time"; NoOfHours = 5; NoOfMinutes = 30;
    SiteDetailListDTO objSiteDetailListDTO = null;
    List<SiteSolrURlListDTO> lstSiteSolrURlListDTO = null;
    string strCountryCode = string.Empty, strCurrenyCode = string.Empty, strLanguageCode = string.Empty,
    strSearchWord = string.Empty, strESearchWord = string.Empty, strIPAddress = string.Empty, strLoginUserName = string.Empty, strLoginCustomerName = string.Empty, UserId = string.Empty, strStoreCode = string.Empty, strCashRegisterName = string.Empty, strFileUploadTypeId = string.Empty;
    int intCountryId = 0, intCurrencyId = 0, intLanguageId = 1, intRoleId = 0, intPageNumber = 0, intPageSize = 0, intSearchTypeId = 0, intRecordCount = 0, intHealthEmployeeTypeId = 0;
    DateTime dtResponseTime = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes);
    string strRequest = string.Empty, strResponse = string.Empty, strFirstName = String.Empty, strLastName = string.Empty, strOrderNumber = string.Empty, strCode = string.Empty, strStatusIds = string.Empty, strMobile = string.Empty, strCode1 = string.Empty, strCode2 = string.Empty, strCode3 = string.Empty;
    DateTime dtfromdate = DateTime.MinValue, dtTodate = DateTime.MinValue, dtAttandencedate = DateTime.MinValue;
    DataSet dsGetSearchData = null;
    string strUserEmail = string.Empty, strHeadersResponse = string.Empty;
    string strTitle = string.Empty, strPlaceholder = string.Empty, strHeaders = string.Empty;
    List<APITemplateColumnListDTO> lstAPITemplateColumnListDTO = null;
    List<StaticAPITemplateColumnListDTO> lstStaticAPITemplateColumnListDTO = null;
    MySqlSearchDataDAL objMySqlSearchDataDAL = null;
    CommonBAL objCommonBAL = null;         //Added by Manisha on 25 July 2024 
    ErrorCodeBAL objErrorCodeBAL = null;   //Added by Manisha on 25 July 2024

    Int64 intUserId = 0, intCustomerId = 0, intId1 = 0; // Added By Fayaz on 15 Oct 2024

    DataTable dtDataTable = null;
    MasterBAL objMasterBAL = null;
    DataTable dtAllSearchData = null;
    DataTable dtFilteredSearchData = null;
    bool IsDashboardAPI = false;
    #endregion

    try
    {
        General.objConfigurationSettingsListDTO = objConfigurationSettingsListDTO;
        General.CreateCodeLog("Step 1.0", "Before calling the Method", objAPIRequest, MethodBase.GetCurrentMethod().Name, lstSiteDetails);
        CommonDAL objCommonDAL = new CommonDAL(objConfigurationSettingsListDTO, string.Empty);
        objRegularExpressions = new SAASPOSRegularExpressions();
        strRequest = JsonConvert.SerializeObject(objAPIRequest);

        #region Checking Validations
        General.CreateCodeLog("Step 1.1", "Before Validating  the Common methods", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

        #region CountryCode
        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 1.2", "Before Validating  the CountryCode", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

            if (!string.IsNullOrEmpty(objAPIRequest.CountryCode))
            {
                strCountryCode = objAPIRequest.CountryCode;

                if (!string.IsNullOrEmpty(strCountryCode))
                {
                    intCountryId = General.GetCountryId(strCountryCode);
                }
                if (intCountryId <= 0)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Country_Code);
                }
                General.CreateCodeLog("Step 1.3", "After Validating  the CountryCode", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
            }
        }
        #endregion

        #region CurrenyCode
        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 1.4", "Before Validating  the CurrencyCode", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

            if (!string.IsNullOrEmpty(objAPIRequest.CurrenyCode))
            {
                strCurrenyCode = objAPIRequest.CurrenyCode;

                if (!string.IsNullOrEmpty(strCurrenyCode))
                {
                    intCurrencyId = General.GetCurrencyId(strCurrenyCode);
                }

                if (intCurrencyId <= 0)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Currency_Code);
                }
            }
            General.CreateCodeLog("Step 1.5", "After Validating  the CurrencyCode", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

        }
        #endregion

        //#region LanguageCode
        //if (ErrorCode == 0)
        //{
        //    General.CreateCodeLog("Step 1.6", "Before Validating  the LanguageCode", "", MethodBase.GetCurrentMethod().Name);

        //    if (!string.IsNullOrEmpty(objAPIRequest.LanguageCode))
        //    {
        //        strLanguageCode = objAPIRequest.LanguageCode;

        //        if (!string.IsNullOrEmpty(strLanguageCode))
        //        {
        //            intLanguageId = General.GetLanguageId(strLanguageCode);
        //        }

        //        if (intLanguageId <= 0)
        //        {
        //            ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Language_Code);
        //        }
        //    }
        //    General.CreateCodeLog("Step 1.7", "After Validating  the LanguageCode", "", MethodBase.GetCurrentMethod().Name);

        //}
        //#endregion

        #region Assign IP Address
        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 1.8", "Before Assigning the IPAddress", "", MethodBase.GetCurrentMethod().Name);

            if (!string.IsNullOrEmpty(objAPIRequest.IPAddress))
            {
                strIPAddress = objAPIRequest.IPAddress;
            }
            else
            {
                strIPAddress = General.GetIP4Address();
            }
            General.CreateCodeLog("Step 1.9", "After Assigning the IPAddress", "", MethodBase.GetCurrentMethod().Name);

        }

        #endregion

        #region Assign Active
        if (ErrorCode == 0)
        {
            if (!string.IsNullOrEmpty(objAPIRequest.Active))
            {
                bool.TryParse(objAPIRequest.Active, out IsActive);

            }
        }
        #endregion

        #region Assign IsDashboardAPI
        if (ErrorCode == 0)
        {
            if (!string.IsNullOrEmpty(objAPIRequest.IsDashboardAPI))
            {
                bool.TryParse(objAPIRequest.IsDashboardAPI, out IsDashboardAPI);

            }
        }
        #endregion

        #region IsCustomer
        if (ErrorCode == 0)
        {
            if (!string.IsNullOrEmpty(objAPIRequest.cpk) && string.IsNullOrWhiteSpace(objAPIRequest.upk))
            {
                IsCustomer = true;
            }
        }
        #endregion

        General.CreateCodeLog("Step 2.0", "After Validating  the Common Validations", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

        #endregion Checking Validations

        #region ValidateToken 
        if (ErrorCode == 0)
        {
            try
            {
                General.CreateCodeLog("Step 2.1", "Before calling the GetSiteDetails", objAPIRequest, MethodBase.GetCurrentMethod().Name, lstSiteDetails);

                if (!string.IsNullOrEmpty(objAPIRequest.JwtToken))
                {
                    objCommonBAL = new CommonBAL(objConfigurationSettingsListDTO);
                    lstSiteDetails = objCommonBAL.GetSiteDetails(objAPIRequest.JwtToken, intCountryId, intCurrencyId, intLanguageId);
                }
                General.CreateCodeLog("Step 2.2", "After calling the GetSiteDetails", objAPIRequest, MethodBase.GetCurrentMethod().Name, lstSiteDetails);
            }
            catch (Exception ex)
            {
                ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
            }
            if (lstSiteDetails != null && lstSiteDetails.Count > 0)
            {
                ErrorCode = lstSiteDetails[0].ReturnCode;

                if (ErrorCode == 0)
                {
                    strRespectiveConnectionString = lstSiteDetails[0].ConnectionString;
                    strSiteToken = lstSiteDetails[0].SecurityToken;
                    dtTokenExpiryTime = lstSiteDetails[0].TokenExpiryTime;
                    strUserPrivateKey = lstSiteDetails[0].UserPrivateKey;
                    SiteCode = lstSiteDetails[0].SiteCode;
                    TimeZone = lstSiteDetails[0].TimeZone;
                    NoOfHours = lstSiteDetails[0].NoOfHours;
                    NoOfMinutes = lstSiteDetails[0].NoOfMinutes;
                }
            }
            else
            {
                ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Token);
            }
        }
        #endregion

        #region GetSiteUser or Customer DetailByPrivateKey
        if (ErrorCode == 0 && !IsCustomer)
        {
            #region GetUserDetailByPrivateKey
            General.CreateCodeLog("Step 2.3", "Before calling UserDAL", "", MethodBase.GetCurrentMethod().Name);
            lstSiteUserDetailByPrivateKey = new List<SiteUserListDTO>();
            if (objAPIRequest.upk != null && objAPIRequest.upk != string.Empty)
            {
                try
                {
                    General.CreateCodeLog("Step 2.4", "Before calling GetSiteUserDetailByPrivateKey", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

                    objCommonBAL = new CommonBAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                    lstSiteUserDetailByPrivateKey = objCommonBAL.GetSiteUserDetailByPrivateKey(objAPIRequest.upk, intCountryId, intCurrencyId, intLanguageId);

                    General.CreateCodeLog("Step 2.5", "After calling GetSiteUserDetailByPrivateKey", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
                }
                catch (Exception ex)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                    General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                }
            }

            if (lstSiteUserDetailByPrivateKey != null && lstSiteUserDetailByPrivateKey.Count > 0)
            {
                if (lstSiteUserDetailByPrivateKey[0].ExpiryDate > RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes))
                {
                    intRoleId = lstSiteUserDetailByPrivateKey[0].RoleId;
                    strLoginUserName = lstSiteUserDetailByPrivateKey[0].UserName;
                    intUserId = lstSiteUserDetailByPrivateKey[0].UserId;
                    intHealthEmployeeTypeId = lstSiteUserDetailByPrivateKey[0].HealthEmployeeTypeId;
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.User_Session_Expired);
                }
            }
            else
            {
                ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_User);
            }
            #endregion
        }
        else if (ErrorCode == 0 && IsCustomer)
        {
            #region GetCustomerDetailByPrivateKey
            General.CreateCodeLog("Step 2.5", "Before Vaidating Customer Details", "", MethodBase.GetCurrentMethod().Name);

            List<SiteCustomerListDTO> lstCustomerDetailByPrivateKey = new List<SiteCustomerListDTO>();

            if (objAPIRequest.cpk != null && objAPIRequest.cpk != string.Empty)
            {
                try
                {
                    General.CreateCodeLog("Step 2.6", "Before calling GetSiteCustomerDetailByPrivateKey", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);

                    objCommonBAL = new CommonBAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                    lstCustomerDetailByPrivateKey = objCommonBAL.GetSiteCustomerDetailByPrivateKey(objAPIRequest.cpk, intCountryId, intCurrencyId, intLanguageId);

                    General.CreateCodeLog("Step 2.7", "After calling GetSiteCustomerDetailByPrivateKey", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                }
                catch (Exception ex)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                    General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                }
            }

            if (lstCustomerDetailByPrivateKey != null && lstCustomerDetailByPrivateKey.Count > 0)
            {
                if (lstCustomerDetailByPrivateKey[0].ExpiryDate > RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes))
                {
                    if (!string.IsNullOrEmpty(lstCustomerDetailByPrivateKey[0].FirstName))
                    {
                        strLoginCustomerName = lstCustomerDetailByPrivateKey[0].FirstName.Trim();
                    }
                    intCustomerId = lstCustomerDetailByPrivateKey[0].CustomerId;

                    if (intCustomerId > 0)
                    {
                        intUserId = intCustomerId;
                        IsAdminView = true;
                    }
                    else
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Customer);
                    }
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Customer_Session_Expired);
                }
            }
            else
            {
                ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Customer);
            }
            #endregion
        }
        #endregion

        #region Language Code validations
        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 1.75", "After validating the LanguageCode", "", MethodBase.GetCurrentMethod().Name);
            if (!string.IsNullOrEmpty(objAPIRequest.LanguageCode))
            {
                objMasterBAL = new MasterBAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                dtDataTable = objMasterBAL.GetMasterDataByCodeInternal(SAASPOSGeneral.MasterDataCodes.Language.ToString(), objAPIRequest.LanguageCode, lstSiteDetails, lstSiteSolrURlListDTO);

                if (dtDataTable != null && dtDataTable.Rows.Count > 0 && dtDataTable.Columns.Contains("Id"))
                {
                    intLanguageId = Convert.ToInt32(dtDataTable.Rows[0]["Id"]);
                }
                // intLanguageId = MasterData(General.MainMasterDataCodes.Language.ToString(), strLanguageCode);

                if (intLanguageId <= 0)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Language_Code);
                }
            }
            General.CreateCodeLog("Step 1.75", "After validating the LanguageCode", "", MethodBase.GetCurrentMethod().Name);
        }
        #endregion

        #region GET API Template Details
        if (ErrorCode == 0 && !objAPIRequest.IsStaticApiTemplate)
        {
            if (string.IsNullOrEmpty(objAPIRequest.strProcedure))
            {
                if (!string.IsNullOrEmpty(objAPIRequest.ModuleCode))
                {
                    General.CreateCodeLog("Step 2.6", "Before calling GetAPITemplateDetails", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

                    APITemplateBAL objAPITemplateBAL = new APITemplateBAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                    lstAPITemplateColumnListDTO = objAPITemplateBAL.GetAPITemplateDetails(null, objAPIRequest.ModuleCode);

                    General.CreateCodeLog("Step 2.7", "After calling GetAPITemplateDetails", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

                    // Retrieve API template details using the module code, and set the ModuleId, procedure name, and check for AES encryption requirement.

                    if (lstAPITemplateColumnListDTO != null && lstAPITemplateColumnListDTO?.Count >= 0)
                    {
                        objAPIRequest.ModuleId = lstAPITemplateColumnListDTO[0].ModuleId;
                        objAPIRequest.strProcedure = lstAPITemplateColumnListDTO[0].SearchProcedureName;
                        int count = lstAPITemplateColumnListDTO.Where(APITemplateColumnListDTO => APITemplateColumnListDTO.IsAesEncrypt).Count();
                        if (count > 0)
                        {
                            ESerachWordRequird = true;
                        }

                    }
                    else
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_ModuleCode);
                    }
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.ModuleCode_Required);
                }
            }
            else
            {
                ESerachWordRequird = objAPIRequest.IsESearchwordRequred;
            }
        }
        else if (ErrorCode == 0 && objAPIRequest.IsStaticApiTemplate)
        {
            if (string.IsNullOrEmpty(objAPIRequest.strProcedure))
            {
                if (!string.IsNullOrEmpty(objAPIRequest.ModuleCode))
                {
                    General.CreateCodeLog("Step 2.6", "Before calling GetAPITemplateDetails", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);//FFFF

                    APITemplateBAL objAPITemplateBAL = new APITemplateBAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                    lstStaticAPITemplateColumnListDTO = objAPITemplateBAL.GetStaticAPITemplateDetails(null, objAPIRequest.MethodName, objAPIRequest.ModuleCode);

                    General.CreateCodeLog("Step 2.7", "After calling GetAPITemplateDetails", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

                    // Retrieve API template details using the module code, and set the ModuleId, procedure name, and check for AES encryption requirement.

                    if (lstStaticAPITemplateColumnListDTO != null && lstStaticAPITemplateColumnListDTO?.Count >= 0)
                    {
                        objAPIRequest.ModuleId = lstStaticAPITemplateColumnListDTO[0].ModuleId;
                        objAPIRequest.strProcedure = lstStaticAPITemplateColumnListDTO[0].StoreporcedureName;
                        int count = lstStaticAPITemplateColumnListDTO.Where(lstStaticAPITemplateColumnListDTO => lstStaticAPITemplateColumnListDTO.IsAesEncrypt).Count();
                        if (count > 0)
                        {
                            ESerachWordRequird = true;
                        }

                    }
                    else
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_ModuleCode);
                    }
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.ModuleCode_Required);
                }
            }
            else
            {
                ESerachWordRequird = objAPIRequest.IsESearchwordRequred;
            }
        }
        #endregion

        #region SearchTypeId Validation
        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 2.8", "Before Validating  the SearchTypeId", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

            if (objAPIRequest.SearchTypeId != null && !String.IsNullOrWhiteSpace(objAPIRequest.SearchTypeId))
            {
                if (!Regex.IsMatch(objAPIRequest.SearchTypeId.Trim(), objRegularExpressions.RegExForId))
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_SearchTypeId);
                }
                else
                {
                    intSearchTypeId = Convert.ToInt32(objAPIRequest.SearchTypeId);
                }
            }
            General.CreateCodeLog("Step 2.9", "After Validating  the SearchTypeId", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
        }
        #endregion

        #region Permission Check
        if (ErrorCode == 0 && !IsCustomer)
        {
            General.CreateCodeLog("Step 3.0", "Before calling Mandatory validations", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
            objCommonBAL = new CommonBAL(strRespectiveConnectionString, EncryptionKey, objConfigurationSettingsListDTO);
            if (objAPIRequest.ModuleId > 0)
            {
                try
                {
                    General.CreateCodeLog("Step 3.1", "Before calling CheckRolePermission", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

                    if (intSearchTypeId == 1)// checking for admin view // added by madhukar b on 01 March 2024  Task No : (REVALSAAS00891)
                    {
                        IsAuthorizedUser = objCommonBAL.CheckRolePermission(intRoleId, objAPIRequest.ModuleId, Convert.ToInt32(SAASPOSGeneral.Permissions.Admin_View), intCountryId, intCurrencyId, intLanguageId);
                        General.CreateCodeLog("Step 3.2", "After calling CheckRolePermission", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
                        IsAdminView = IsAuthorizedUser;

                        if (!IsAuthorizedUser)
                        {
                            intSearchTypeId = 0;
                        }
                    }
                    else if (intSearchTypeId == 0)
                    {

                        General.CreateCodeLog("Step 3.3", "Before calling CheckRolePermission", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
                        IsAuthorizedUser = objCommonBAL.CheckRolePermission(intRoleId, objAPIRequest.ModuleId, Convert.ToInt32(SAASPOSGeneral.Permissions.View), intCountryId, intCurrencyId, intLanguageId);
                        General.CreateCodeLog("Step 3.4", "After calling CheckRolePermission", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

                        if (objAPIRequest.ModuleId == Convert.ToInt32(SAASPOSGeneral.Module.Product))
                        {
                            if (!IsAuthorizedUser)
                            {
                                objAPIRequest.ModuleId = Convert.ToInt32(SAASPOSGeneral.Module.StoreProduct);
                                IsAuthorizedUser = objCommonBAL.CheckRolePermission(intRoleId, objAPIRequest.ModuleId, Convert.ToInt32(SAASPOSGeneral.Permissions.View), intCountryId, intCurrencyId, intLanguageId);
                            }

                        }

                    }
                    else
                    {
                        General.CreateCodeLog("Step 3.3", "Before calling CheckRolePermission", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
                        IsAuthorizedUser = objCommonBAL.CheckRolePermission(intRoleId, objAPIRequest.ModuleId, Convert.ToInt32(SAASPOSGeneral.Permissions.View), intCountryId, intCurrencyId, intLanguageId);
                        General.CreateCodeLog("Step 3.4", "After calling CheckRolePermission", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
                    }
                }
                catch (Exception Ex)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                    General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                }
                finally
                {
                    objCommonBAL = null;
                }
            }

            if (!IsAuthorizedUser)
            {
                ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Do_Not_Have_Permission);

                //Added below condition By Srivathsava on 17 May 2024
                // For the Country module, role permissions are not being checked.
                // The Country module is unpublished in the UI and does not require role permission checks.
                if (ErrorCode != 0 && !IsAuthorizedUser && objAPIRequest.ModuleId == Convert.ToInt32(SAASPOSGeneral.Module.Country))
                {
                    IsAuthorizedUser = true;
                    ErrorCode = 0;
                }
            }
        }
        #endregion Permission Check

        #region Request data Validations

        #region OrderNumber Validation
        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 3.5", "Before Validating  OrderNumber", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

            if (objAPIRequest.OrderNumber != null && !string.IsNullOrWhiteSpace(objAPIRequest.OrderNumber))
            {
                if (!Regex.IsMatch(objAPIRequest.OrderNumber.Trim(), objRegularExpressions.RegexForOrderNumber))
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_OrderNumber);
                }
                else
                {
                    strOrderNumber = objAPIRequest.OrderNumber;
                }
            }
            General.CreateCodeLog("Step 3.6", "After Validating  OrderNumber", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

        }
        #endregion

        #region FirstName
        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 3.7", "Before Validating  FirstName", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

            if (objAPIRequest.FirstName != null && !string.IsNullOrWhiteSpace(objAPIRequest.FirstName))
            {
                if (!Regex.IsMatch(objAPIRequest.FirstName.Trim(), objRegularExpressions.RegExForName))
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_FirstName);
                }
                else
                {
                    strFirstName = objAPIRequest.FirstName;
                }
            }
            General.CreateCodeLog("Step 3.7", "After Validating  FirstName", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

        }
        #endregion

        #region LastName
        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 3.8", "Before Validating  LastName", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
            if (objAPIRequest.LastName != null && !string.IsNullOrWhiteSpace(objAPIRequest.LastName))
            {
                if (!Regex.IsMatch(objAPIRequest.LastName.Trim(), objRegularExpressions.RegExForName))
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Last_Name);
                }
                else
                {
                    strLastName = objAPIRequest.LastName;
                }
            }
            General.CreateCodeLog("Step 3.9", "After Validating  LastName", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
        }
        #endregion

        #region FromDate
        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 4.0", "Before Validating  FromDate", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

            if (!String.IsNullOrWhiteSpace(objAPIRequest.FromDate))
            {
                if (!Regex.IsMatch(objAPIRequest.FromDate.Trim(), objRegularExpressions.RegExforDate))
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_FromDate);
                }
                else
                {
                    if (DateTime.TryParseExact(objAPIRequest.FromDate.Trim(), objConfigurationSettingsListDTO.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dtfromdate))
                    {
                        DateTime.TryParseExact(objAPIRequest.FromDate.Trim(), objConfigurationSettingsListDTO.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dtfromdate);
                    }
                    else
                    {
                        try
                        {
                            dtfromdate = Convert.ToDateTime(objAPIRequest.FromDate.Trim());
                        }
                        catch (Exception Ex)
                        {
                            dtfromdate = DateTime.MinValue;
                            ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                            General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                        }
                    }
                    //DateTime.TryParse(objAPIRequest.FromDate.Trim(), out dtfromdate);
                }
            }
            General.CreateCodeLog("Step 4.1", "After Validating  FromDate", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

        }

        #endregion

        #region ToDate
        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 4.2", "Before Validating  ToDate", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

            if (!String.IsNullOrWhiteSpace(objAPIRequest.ToDate))
            {
                if (!Regex.IsMatch(objAPIRequest.ToDate.Trim(), objRegularExpressions.RegExforDate))
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Todate);
                }
                else
                {
                    if (DateTime.TryParseExact(objAPIRequest.ToDate.Trim(), objConfigurationSettingsListDTO.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dtTodate))
                    {
                        DateTime.TryParseExact(objAPIRequest.ToDate.Trim(), objConfigurationSettingsListDTO.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dtTodate);
                    }
                    else
                    {
                        try
                        {
                            dtTodate = Convert.ToDateTime(objAPIRequest.ToDate.Trim());
                        }
                        catch (Exception Ex)
                        {
                            dtTodate = DateTime.MinValue;
                            ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                            General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                        }
                    }
                    //DateTime.TryParse(objAPIRequest.ToDate.Trim(), out dtTodate);
                }
            }
            General.CreateCodeLog("Step 4.3", "After Validating  ToDate", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

        }

        if (objAPIRequest.FromDate != null && objAPIRequest.ToDate != null)
        {
            if (dtfromdate > dtTodate)
            {
                ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.FromDate_Should_not_be_greater_than_ToDate);
            }

        }

        #endregion

        #region Search Text Validation
        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 4.4", "Before Validating  SearchWord", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

            if (objAPIRequest.SearchWord != null && !String.IsNullOrWhiteSpace(objAPIRequest.SearchWord))
            {
                if (!Regex.IsMatch(objAPIRequest.SearchWord.Trim(), objRegularExpressions.RegExSearchWord))
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_SearchWord);
                }
                else
                {
                    strSearchWord = objAPIRequest.SearchWord;
                    if (ESerachWordRequird)
                    {
                        strESearchWord = General.RevalEncrypt(objAPIRequest.SearchWord, clsCryptography.Decrypt(objConfigurationSettingsListDTO.AESEncryptkey, objConfigurationSettingsListDTO.EncryptionKey), clsCryptography.Decrypt(objConfigurationSettingsListDTO.AESEncryptiv, objConfigurationSettingsListDTO.EncryptionKey));
                    }
                }
            }
            General.CreateCodeLog("Step 4.5", "After Validating  SearchWord", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

        }
        #endregion

        #region  PageNumber Validation
        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 4.6", "Before Validating  PageNumber", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

            if (objAPIRequest.PageNumber != null && !String.IsNullOrWhiteSpace(objAPIRequest.PageNumber))
            {
                if (!Regex.IsMatch(objAPIRequest.PageNumber.Trim(), objRegularExpressions.RegExForId))
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_PageNumber);
                }
                else
                {
                    intPageNumber = Convert.ToInt32(objAPIRequest.PageNumber);
                }
            }
            General.CreateCodeLog("Step 4.7", "After Validating  PageNumber", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

        }
        #endregion

        #region  PageSize Validation
        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 4.8", "Before Validating  PageSize", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

            if (objAPIRequest.PageSize != null && !String.IsNullOrWhiteSpace(objAPIRequest.PageSize))
            {
                if (!Regex.IsMatch(objAPIRequest.PageSize.Trim(), objRegularExpressions.RegExForId))
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_PageSize);
                }
                else
                {
                    intPageSize = Convert.ToInt32(objAPIRequest.PageSize);
                }
            }
            General.CreateCodeLog("Step 4.9", "After Validating  PageSize", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
        }
        #endregion

        #region StoreCode Validation
        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 5.0", "Before Validating  StoreCode", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

            if (objAPIRequest.StoreCode != null && !String.IsNullOrWhiteSpace(objAPIRequest.StoreCode))
            {
                if (!Regex.IsMatch(objAPIRequest.StoreCode.Trim(), objRegularExpressions.RegExForCode))
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_StoreCode);
                }
                else
                {
                    strStoreCode = objAPIRequest.StoreCode;
                }
            }
            General.CreateCodeLog("Step 5.1", "After Validating  StoreCode", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails); ;

        }
        #endregion

        #region CashRegisterName Validation
        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 5.2", "Before Validating  CashRegisterName", "", MethodBase.GetCurrentMethod().Name);

            if (objAPIRequest.CashRegisterName != null && !String.IsNullOrWhiteSpace(objAPIRequest.CashRegisterName))
            {
                if (!Regex.IsMatch(objAPIRequest.CashRegisterName.Trim(), objRegularExpressions.RegExForName))
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_CashRegisterName);
                }
                else
                {
                    strCashRegisterName = objAPIRequest.CashRegisterName;
                }
            }
            General.CreateCodeLog("Step 5.3", "After Validating  CashRegisterName", "", MethodBase.GetCurrentMethod().Name);
        }

        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 5.4", "Before Validating  RecordCount", "", MethodBase.GetCurrentMethod().Name);

            if (objAPIRequest.RecordCount != null && !String.IsNullOrWhiteSpace(objAPIRequest.RecordCount))
            {
                if (!Regex.IsMatch(objAPIRequest.RecordCount.Trim(), objRegularExpressions.RegExForId))
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_RecordCount);
                }
                else
                {
                    intRecordCount = Convert.ToInt32(objAPIRequest.RecordCount);
                }
            }
            General.CreateCodeLog("Step 5.5", "After Validating  RecordCount", "", MethodBase.GetCurrentMethod().Name);

        }

        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 5.6", "Before Validating  Code", "", MethodBase.GetCurrentMethod().Name);

            if (objAPIRequest.Code != null && !String.IsNullOrWhiteSpace(objAPIRequest.Code))
            {
                if (!Regex.IsMatch(objAPIRequest.Code.Trim(), objRegularExpressions.RegExForCode))
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Code);
                }
                else
                {
                    strCode = objAPIRequest.Code;
                }
            }
            General.CreateCodeLog("Step 5.7", "After Validating  Code", "", MethodBase.GetCurrentMethod().Name);

        }

        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 5.8", "Before Validating  StatusIds", "", MethodBase.GetCurrentMethod().Name);

            if (objAPIRequest.StatusIds != null && !String.IsNullOrWhiteSpace(objAPIRequest.StatusIds))
            {
                if (!Regex.IsMatch(objAPIRequest.StatusIds.Trim(), objRegularExpressions.RegExIds))
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_StatusIds);
                }
                else
                {
                    strStatusIds = objAPIRequest.StatusIds;
                }
            }
            General.CreateCodeLog("Step 5.9", "After Validating  StatusIds", "", MethodBase.GetCurrentMethod().Name);

        }

        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 6.0", "Before Validating  Mobile", "", MethodBase.GetCurrentMethod().Name);

            if (objAPIRequest.Mobile != null && !String.IsNullOrWhiteSpace(objAPIRequest.Mobile))
            {
                if (!Regex.IsMatch(objAPIRequest.Mobile.Trim(), objRegularExpressions.RegExForId))
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Mobile);
                }
                else
                {
                    strMobile = objAPIRequest.Mobile;
                }
            }
            General.CreateCodeLog("Step 6.1", "After Validating  Mobile", "", MethodBase.GetCurrentMethod().Name);

        }

        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 6.2", "Before Validating  Mobile", "", MethodBase.GetCurrentMethod().Name);

            if (objAPIRequest.UserEmail != null && !String.IsNullOrWhiteSpace(objAPIRequest.UserEmail))
            {
                if (!Regex.IsMatch(objAPIRequest.UserEmail.Trim(), objRegularExpressions.RegExForEmail))
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Email);
                }
                else
                {
                    strUserEmail = objAPIRequest.UserEmail;
                }
            }
            General.CreateCodeLog("Step 6.3", "After Validating  Mobile", "", MethodBase.GetCurrentMethod().Name);

        }

        #region AttandenceDate
        //if (ErrorCode == 0)
        //{
        //    if (!String.IsNullOrWhiteSpace(objAPIRequest.AttendanceDate))
        //    {
        //        if (!Regex.IsMatch(objAPIRequest.AttendanceDate.Trim(), objRegularExpressions.RegExforDate))
        //        {
        //            ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_AttendanceDate);
        //        }
        //        else
        //        {
        //            try
        //            {
        //                dtAttandencedate = Convert.ToDateTime(objAPIRequest.AttendanceDate.Trim());
        //                if (dtAttandencedate > DateTime.Now.Date)
        //                {
        //                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.AttendanceDate_GreaterThanPresent);
        //                }

        //            }
        //            catch (Exception Ex)
        //            {
        //                dtAttandencedate = DateTime.MinValue;

        //                ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
        //                General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.AttendanceDate_Required);
        //    }
        //}

        #endregion

        #endregion

        #region Code Validations
        if (ErrorCode == 0)
        {
            if (!string.IsNullOrWhiteSpace(objAPIRequest.Code1))
            {
                strCode1 = objAPIRequest.Code1;
            }
        }

        if (ErrorCode == 0)
        {
            if (!string.IsNullOrWhiteSpace(objAPIRequest.Code2))
            {
                strCode2 = objAPIRequest.Code2;
            }
        }

        if (ErrorCode == 0)
        {
            if (!string.IsNullOrWhiteSpace(objAPIRequest.Code3))
            {
                strCode3 = objAPIRequest.Code3;
            }
        }
        #endregion

        #endregion

        #region GetAttendanceReport Request Validations
        if (ErrorCode == 0)
        {
            if (objAPIRequest.ModuleId == Convert.ToInt32(SAASPOSGeneral.Module.Daily_Attendance_Report))
            {
                General.CreateCodeLog("Step 6.4", "Before Validating  Code1", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

                if (!string.IsNullOrEmpty(objAPIRequest.Code1))
                {
                    if (!Regex.IsMatch(objAPIRequest.Code1.Trim(), objRegularExpressions.RegExForMonth))
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Month);
                    }
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Month_Required);
                }
                General.CreateCodeLog("Step 6.5", "After Validating  Code1", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
                General.CreateCodeLog("Step 6.6", "Before Validating  Code2", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
                if (!string.IsNullOrEmpty(objAPIRequest.Code2))
                {
                    if (!Regex.IsMatch(objAPIRequest.Code2.Trim(), objRegularExpressions.RegExForYear))
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Year);
                    }
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Year_Required);
                }
                General.CreateCodeLog("Step 6.7", "After Validating  Code2", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

            }
        }
        #endregion

        #region IsFromMegaMenu Value Get for GetMenuDetails
        if (ErrorCode == 0)
        {
            if (objAPIRequest.ModuleId == Convert.ToInt32(SAASPOSGeneral.Module.Mega_Menu))
            {
                try
                {
                    General.CreateCodeLog("Step 6.4", "Before Calling GetSiteConfiguration for IsFromMegaMenu", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
                    objCommonBAL = new CommonBAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                    object objIsFromMegaMenu = objCommonBAL.GetSiteConfiguration(Convert.ToInt32(SAASPOSGeneral.SiteConfiguration.IsFromMegaMenu), intCountryId, intLanguageId, intCurrencyId);

                    if (objIsFromMegaMenu != null)
                    {
                        bool.TryParse(objIsFromMegaMenu.ToString(), out IsFromMegaMenu);
                        intId1 = IsFromMegaMenu ? 1 : 0;
                    }
                    General.CreateCodeLog("Step 6.5", "After Calling GetSiteConfiguration for IsFromMegaMenu", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
                }
                catch (Exception ex)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                    General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                }
            }
        }
        #endregion

        //Added by Abhijit S on 12 Nov 2025
        //For Getting only IsPublished = true records in the response when IsCheckAuthenticate is false
        if (!objConfigurationSettingsListDTO.IsCheckAuthenticate)
        {
            IsActive = true;
        }


        #region Calling DAL
        if (ErrorCode == 0)
        {
            General.CreateCodeLog("Step 6.8", "Before calling the DAL", objAPIRequest, MethodBase.GetCurrentMethod().Name, lstSiteDetails);
            lstSearchDataListDTO = new List<SearchDataListDTO>();
            if (ErrorCode == 0)
            {
                if (intPageNumber <= 0 || intPageSize <= 0)
                {
                    intPageNumber = 1;
                    intPageSize = 999999999;
                }
                objSearchListDTO = new CommonSearchListDTO()
                {
                    StoredProcedureName = objAPIRequest.strProcedure,
                    PageSize = intPageSize,
                    PageNumber = intPageNumber,
                    SearchWord = strSearchWord,
                    RecordCount = intRecordCount,
                    CountryId = intCountryId,
                    CurrencyId = intCurrencyId,
                    LanguageId = intLanguageId,
                    Code = strCode,
                    SearchTypeId = intSearchTypeId,
                    StatusIds = strStatusIds,
                    FromDate = dtfromdate,
                    ToDate = dtTodate,
                    Mobile = strMobile,
                    UserEmail = strUserEmail,
                    OrderNumber = strOrderNumber,
                    IsPublished = IsActive,
                    StoreCode = strStoreCode,
                    CashRegisterName = strCashRegisterName,
                    UserId = intUserId,
                    Code1 = strCode1,
                    Code2 = strCode2,
                    Code3 = strCode3,
                    ESearchWord = strESearchWord,
                    Id1 = intId1,
                    ModuleId = objAPIRequest.ModuleId,
                    IsAdminView = IsAdminView,
                    HealthEmployeeTypeId = intHealthEmployeeTypeId,
                    IsDashboardAPI = IsDashboardAPI
                    //AttendanceDate = dtAttandencedate
                };
            }

            if (objSearchListDTO != null)
            {
                try
                {
                    General.CreateCodeLog("Step 6.9", "Starting of GetDataBySearchDB DAL calling - Response", objSearchListDTO, MethodBase.GetCurrentMethod().Name, lstSiteDetails);
                    if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(SAASPOSGeneral.DataBaseTypeId.MsSql))
                    {
                        objSearchDataDAL = new SearchDataDAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                        dsGetSearchData = objSearchDataDAL.GetDataBySearchDB(objSearchListDTO);
                    }
                    else if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(SAASPOSGeneral.DataBaseTypeId.MySql))
                    {
                        objMySqlSearchDataDAL = new MySqlSearchDataDAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                        dsGetSearchData = objMySqlSearchDataDAL.GetDataBySearchDB(objSearchListDTO);
                    }
                    General.CreateCodeLog("Step 7.0", "Ending of GetDataBySearchDB DAL calling - Response", lstSearchDataListDTO, MethodBase.GetCurrentMethod().Name, lstSiteDetails);

                    if (dsGetSearchData != null && dsGetSearchData.Tables.Count > 0)
                    {
                        //lstSearchResultList = new List<string>();
                        //lstSearchResultList = dsGetSearchData.Tables[0].AsEnumerable().Select(m => m.Field<string>("Headers")).ToList();

                        strHeaders = JsonConvert.SerializeObject(dsGetSearchData.Tables[0], Formatting.Indented);

                        General.DecryptDataTable(dsGetSearchData.Tables[1], clsCryptography.Decrypt(objConfigurationSettingsListDTO.AESEncryptkey, objConfigurationSettingsListDTO.EncryptionKey),
                                                                        clsCryptography.Decrypt(objConfigurationSettingsListDTO.AESEncryptiv, objConfigurationSettingsListDTO.EncryptionKey));

                        strResponse = JsonConvert.SerializeObject(dsGetSearchData.Tables[1], Formatting.Indented);


                        #region Commented by Akhila M on 16 Aug 2023

                        //dynamic dyJObjects = dsGetSearchData.Tables.AsEnumerable().Cast<dynamic>().ToList().ElementAt(0);
                        //strResponse = JsonConvert.SerializeObject(dyJObjects.Table);

                        #endregion
                    }
                    else
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.No_Records_Found);
                    }
                }
                catch (Exception Ex)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                    General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                }
                finally
                {
                    objSearchListDTO = null;
                    objSearchDataDAL = null;
                    objMySqlSearchDataDAL = null;
                }
            }
        }
        #endregion
    }
    catch (Exception ex)
    {
        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
        General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
    }
    finally
    {
        List<ErrorCodeListDTO> lstErrorCodeListDTO = null;
        try
        {
            if (ErrorCode > 0)
            {
                try
                {
                    objErrorCodeBAL = new ErrorCodeBAL(objConfigurationSettingsListDTO, strRespectiveConnectionString, EncryptionKey);
                    General.CreateCodeLog("Step 7.1", "Satrting of GetErrorCodeById calling - Response", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
                    lstErrorCodeListDTO = objErrorCodeBAL.GetErrorCodeById(objConfigurationSettingsListDTO, ErrorCode, strRespectiveConnectionString, intLanguageId);
                    General.CreateCodeLog("Step 7.2", "Ending of GetErrorCodeById calling - Response", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
                }
                catch (Exception Ex)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                    General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                }
                finally
                {
                    objErrorCodeBAL = null;
                }

                if (lstErrorCodeListDTO != null && lstErrorCodeListDTO.Count > 0)
                {
                    objResponse = new Response<object>();
                    objResponse.ReturnCode = lstErrorCodeListDTO[0].ReturnCode;
                    objResponse.ReturnMessage = lstErrorCodeListDTO[0].ReturnMessage;
                    objResponse.Data = null;
                }
            }
            else if (ErrorCode == 0 && dsGetSearchData != null && dsGetSearchData.Tables.Count > 0)
            {
                objResponse = new Response<object>();
                objResponse.ReturnCode = 0;
                objResponse.ReturnMessage = "success";
                objResponse.RecordCount = dsGetSearchData.Tables[1].Rows.Count;
                objResponse.ResponseTime = Math.Round((RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes) - startResponseTime).TotalMilliseconds).ToString();
                objResponse.Headers = JsonConvert.DeserializeObject<dynamic>(strHeaders);
                var json = JsonConvert.DeserializeObject<dynamic>(strResponse);
                objResponse.Data = json;
            }

            #region Inserting In APILog 
            objAPILogDetailListDTO = new APILogDetailListDTO();
            objAPILogDetailListDTO.RequestXML = strRequest;
            objAPILogDetailListDTO.ResponseXML = JsonConvert.SerializeObject(objResponse);
            objAPILogDetailListDTO.DateCreated = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes);
            objAPILogDetailListDTO.MethodName = MethodBase.GetCurrentMethod().Name;
            objAPILogDetailListDTO.APIMethodId = Convert.ToInt32(SAASPOSGeneral.APIMethod.GetUserDetailsBySearch);
            objAPILogDetailListDTO.IsRequest = true;
            objAPILogDetailListDTO.ApiLogTypeId = objConfigurationSettingsListDTO.APILogTypeId;
            General.objConfigurationSettingsListDTO = objConfigurationSettingsListDTO;

            if (objAPILogDetailListDTO != null && strRespectiveConnectionString != null && strRespectiveConnectionString != string.Empty)
            {
                try
                {
                    General.CreateCodeLog("Step 7.3", "Satrting of InsertAPILog calling - Response", objAPILogDetailListDTO, MethodBase.GetCurrentMethod().Name);
                    Task tskInsert = Task.Run(() =>
                    {
                        long[] intAPILog = General.InsertAPILog(objAPILogDetailListDTO, strRespectiveConnectionString, objConfigurationSettingsListDTO.EncryptionKey, objConfigurationSettingsListDTO.LogTypeId, objConfigurationSettingsListDTO.CodeLogRequired, lstSiteDetails, lstSiteSolrURlListDTO);
                    });
                    General.CreateCodeLog("Step 7.4", "Ending of InsertAPILog calling - Response", "", MethodBase.GetCurrentMethod().Name);
                }
                catch (Exception Ex)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                    General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                }
                finally
                {
                    objAPILogDetailListDTO = null;
                }
            }
            #endregion

            objResponse.ResponseTime = Math.Round((RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes) - startResponseTime).TotalMilliseconds).ToString();
        }
        catch (Exception ex)
        {
            General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
        }
        finally
        {
            #region Nullifying Objects
            objSearchDataDAL = null;
            lstSearchDataListDTO = null;
            objSearchListDTO = null;
            lstErrorCodeListDTO = null;
            objErrorCodeBAL = null;
            #endregion
        }

    }
    return objResponse;
}
#endregion












#region GetDataByCode
//*********************************************************************************************************
//Purpose            :  This API Method will get the Get Data By Code.
//Layer	             :  API
//Method Name        :	GetDataByCode
//Input Parameters   :  
//Return Values      :  
// --------------------------------------------------------------------------------------------------------
//    Version            Author                     Date               Remarks       
//  -------------------------------------------------------------------------------------------------------
//    1.0	            Rajesh K                 09 Aug 2023        Creation
//    1.1               Prakash T                03 Oct 2025        RDLC - REV0036/MS/RDLC/865 -Common dll separation changes - Revalsys.SAASPOSCommon Reference Added
//                                                                  and using SAASPOSGeneral for enums and using SAASPOSRegularExpressions.cs for Regex validation
//    1.2               Abhijit S                 07 Now 2025       Added Condition For Getting only IsPublished = true records in the response when IsCheckAuthenticate is false
//*********************************************************************************************************
/// <summary>
/// This API Method will get the Get Data By Code.
/// </summary>
/// <param name=""></param>
/// <returns>ContentResult</returns>
/// 
public Response<object> GetDataByCode(SearchCodeListDTO objAPIRequest, string[] TabeNames = null, List<string> lstTableNames = null)
{
    DateTime startTime = DateTime.MinValue; bool IsMicrosoftInsightsRequired = false;
    Stopwatch timer = null; object objApplicationInsights = null;

    startTime = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes);
    timer = System.Diagnostics.Stopwatch.StartNew();

    #region Common Variables
    DateTime startResponseTime = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes);
    int ErrorCode = 0;
    APILogDetailListDTO objAPILogDetailListDTO = null;
    List<SiteDetails> lstSiteDetails = null;
    List<SiteUserListDTO> lstSiteUserDetailByPrivateKey = null;
    string strRespectiveConnectionString = string.Empty;
    string strSiteToken = string.Empty;
    DateTime dtTokenExpiryTime = DateTime.MinValue, dtExpiryTime = DateTime.MinValue;
    string strUserPrivateKey = string.Empty;
    string strRequest = string.Empty;
    #endregion

    #region Specific Variables
    Response<object> objResponse = null;
    CommonSearchListDTO objSearchListDTO = null;
    List<SearchDataListDTO> lstSearchDataListDTO = null;
    SAASPOSRegularExpressions objRegularExpressions = null;
    SearchDataDAL objSearchDataDAL = null;
    bool IsAuthorizedUser = false, IsCustomer = false;
    TimeZone = "Indian Standard Time"; NoOfHours = 5; NoOfMinutes = 30;
    SiteDetailListDTO objSiteDetailListDTO = null;
    List<SiteSolrURlListDTO> lstSiteSolrURlListDTO = null;
    string strCountryCode = string.Empty, strCurrenyCode = string.Empty, strLanguageCode = string.Empty,
    strSearchWord = string.Empty, strIPAddress = string.Empty, strLoginUserName = string.Empty, strLoginCustomerName = string.Empty, UserId = string.Empty;
    int intCountryId = 0, intCurrencyId = 0, intLanguageId = 1, intRoleId = 0;
    Int64 intUserId = 0;
    DateTime dtResponseTime = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes);
    string strResponse = string.Empty, strFirstName = String.Empty, strLastName = string.Empty, strOrderNumber = string.Empty, strCode = string.Empty;
    DateTime dtfromdate = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes), dtToday = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes);
    DataSet dsGetSearchData = null;
    List<APITemplateColumnListDTO> lstAPITemplateColumnListDTO = null;
    List<APITemplateColumnListDTO> lstChildAPITemplateColumn = null;
    List<StaticAPITemplateColumnListDTO> lstStaticAPITemplateColumnListDTO = null;
    MySqlSearchDataDAL objMySqlSearchDataDAL = null;
    CommonBAL objCommonBAL = null;         //Added by Manisha on 25 July 2024 
    ErrorCodeBAL objErrorCodeBAL = null;   //Added by Manisha on 25 July 2024

    DataTable dtDataTable = null;
    MasterBAL objMasterBAL = null;
    DataTable dtAllSearchData = null;
    DataTable dtFilteredSearchData = null;
    #endregion

    try
    {
        if (objAPIRequest != null)
        {
            #region Adding Tablenames to List
            lstTableNames = new List<string>();

            if (TabeNames != null && TabeNames.Length > 0)
            {
                for (int i = 0; i < TabeNames.Length; i++)
                {
                    string strTableName = TabeNames[i];

                    if (!lstTableNames.Contains(strTableName))
                    {
                        lstTableNames.Add(strTableName);
                    }
                }
            }
            #endregion

            strRequest = JsonConvert.SerializeObject(objAPIRequest);
            General.objConfigurationSettingsListDTO = objConfigurationSettingsListDTO;
            General.CreateCodeLog("Step 1.1", "Before calling the Method", objAPIRequest, MethodBase.GetCurrentMethod().Name, lstSiteDetails);
            CommonDAL objCommonDAL = new CommonDAL(objConfigurationSettingsListDTO, string.Empty);
            objRegularExpressions = new SAASPOSRegularExpressions();

            #region Checking Validations 
            General.CreateCodeLog("Step 1.2", "Before Validating the Method", objAPIRequest, MethodBase.GetCurrentMethod().Name, lstSiteDetails);

            #region CountryCode
            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 1.3", "Before Validating the CountryCode", objAPIRequest, MethodBase.GetCurrentMethod().Name);

                if (!string.IsNullOrEmpty(objAPIRequest.CountryCode))
                {
                    strCountryCode = objAPIRequest.CountryCode;

                    if (!string.IsNullOrEmpty(strCountryCode))
                    {
                        intCountryId = General.GetCountryId(strCountryCode);
                    }
                    if (intCountryId <= 0)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Country_Code);
                    }
                }
                General.CreateCodeLog("Step 1.4", "After Validating the CountryCode", objAPIRequest, MethodBase.GetCurrentMethod().Name);

            }
            #endregion

            #region CurrenyCode
            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 1.5", "Before Validating the CurrencyCode", objAPIRequest, MethodBase.GetCurrentMethod().Name);

                if (!string.IsNullOrEmpty(objAPIRequest.CurrenyCode))
                {
                    strCurrenyCode = objAPIRequest.CurrenyCode;

                    if (!string.IsNullOrEmpty(strCurrenyCode))
                    {
                        intCurrencyId = General.GetCurrencyId(strCurrenyCode);
                    }

                    if (intCurrencyId <= 0)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Currency_Code);
                    }
                }
                General.CreateCodeLog("Step 1.6", "After Validating the CurrencyCode", objAPIRequest, MethodBase.GetCurrentMethod().Name);

            }
            #endregion

            //#region LanguageCode
            //if (ErrorCode == 0)
            //{
            //    General.CreateCodeLog("Step 1.7", "Before Validating the LanguageCode", objAPIRequest, MethodBase.GetCurrentMethod().Name);

            //    if (!string.IsNullOrEmpty(objAPIRequest.LanguageCode))
            //    {
            //        strLanguageCode = objAPIRequest.LanguageCode;

            //        if (!string.IsNullOrEmpty(strLanguageCode))
            //        {
            //            intLanguageId = General.GetLanguageId(strLanguageCode);
            //        }

            //        if (intLanguageId <= 0)
            //        {
            //            ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Language_Code);
            //        }
            //    }
            //    General.CreateCodeLog("Step 1.8", "After Validating the LanguageCode", objAPIRequest, MethodBase.GetCurrentMethod().Name);

            //}
            //#endregion

            #region Assign IP Address
            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 1.9", "Before assinging  the IPAddress", objAPIRequest, MethodBase.GetCurrentMethod().Name);

                if (!string.IsNullOrEmpty(objAPIRequest.IPAddress))
                {
                    strIPAddress = objAPIRequest.IPAddress;
                }
                else
                {
                    strIPAddress = General.GetIP4Address();
                }
                General.CreateCodeLog("Step 2.0", "Before assinging  the IPAddress", objAPIRequest, MethodBase.GetCurrentMethod().Name);
            }
            #endregion

            #region IsCustomer
            if (ErrorCode == 0)
            {
                if (!string.IsNullOrEmpty(objAPIRequest.cpk) && string.IsNullOrWhiteSpace(objAPIRequest.upk))
                {
                    IsCustomer = true;
                }
            }
            #endregion
            General.CreateCodeLog("Step 2.1", "After Validating  the Common Methods", objAPIRequest, MethodBase.GetCurrentMethod().Name, lstSiteDetails);

            #endregion Checking Validations

            #region ValidateToken 
            if (ErrorCode == 0)
            {
                try
                {
                    General.CreateCodeLog("Step 2.2", "Before calling the GetSiteDetails", objAPIRequest, MethodBase.GetCurrentMethod().Name, lstSiteDetails);

                    if (!string.IsNullOrEmpty(objAPIRequest.JwtToken))
                    {
                        objCommonBAL = new CommonBAL(objConfigurationSettingsListDTO);
                        lstSiteDetails = objCommonBAL.GetSiteDetails(objAPIRequest.JwtToken, intCountryId, intCurrencyId, intLanguageId);
                    }
                    General.CreateCodeLog("Step 2.3", "After calling the GetSiteDetails", objAPIRequest, MethodBase.GetCurrentMethod().Name, lstSiteDetails);
                }
                catch (Exception ex)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                    General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                }

                if (lstSiteDetails != null && lstSiteDetails.Count > 0)
                {
                    ErrorCode = lstSiteDetails[0].ReturnCode;

                    //assigning site details from list to respective variables

                    if (ErrorCode == 0)
                    {
                        strRespectiveConnectionString = lstSiteDetails[0].ConnectionString;
                        strSiteToken = lstSiteDetails[0].SecurityToken;
                        dtTokenExpiryTime = lstSiteDetails[0].TokenExpiryTime;
                        strUserPrivateKey = lstSiteDetails[0].UserPrivateKey;
                        SiteCode = lstSiteDetails[0].SiteCode;
                        TimeZone = lstSiteDetails[0].TimeZone;
                        NoOfHours = lstSiteDetails[0].NoOfHours;
                        NoOfMinutes = lstSiteDetails[0].NoOfMinutes;
                    }
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Token);
                }
            }
            #endregion

            #region GetSiteUser or Customer DetailByPrivateKey
            if (ErrorCode == 0 && !IsCustomer)
            {
                #region GetUserDetailByPrivateKey
                General.CreateCodeLog("Step 2.4", "Before calling UserDAL", "", MethodBase.GetCurrentMethod().Name);
                lstSiteUserDetailByPrivateKey = new List<SiteUserListDTO>();
                if (objAPIRequest.upk != null && objAPIRequest.upk != string.Empty)
                {
                    try
                    {
                        General.CreateCodeLog("Step 2.5", "Before calling GetSiteUserDetailByPrivateKey", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);

                        objCommonBAL = new CommonBAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                        lstSiteUserDetailByPrivateKey = objCommonBAL.GetSiteUserDetailByPrivateKey(objAPIRequest.upk, intCountryId, intCurrencyId, intLanguageId);

                        General.CreateCodeLog("Step 2.6", "After calling GetSiteUserDetailByPrivateKey", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
                    }
                    catch (Exception ex)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                        General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                    }
                }

                if (lstSiteUserDetailByPrivateKey != null && lstSiteUserDetailByPrivateKey.Count > 0)
                {
                    if (lstSiteUserDetailByPrivateKey[0].ExpiryDate > RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes))
                    {
                        intRoleId = lstSiteUserDetailByPrivateKey[0].RoleId;
                        strLoginUserName = lstSiteUserDetailByPrivateKey[0].UserName;
                        intUserId = lstSiteUserDetailByPrivateKey[0].UserId;
                    }
                    else
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.User_Session_Expired);
                    }
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_User);
                }
                #endregion
            }
            else if (ErrorCode == 0 && IsCustomer)
            {
                #region GetCustomerDetailByPrivateKey
                General.CreateCodeLog("Step 2.5", "Before Vaidating Customer Details", "", MethodBase.GetCurrentMethod().Name);

                List<SiteCustomerListDTO> lstCustomerDetailByPrivateKey = new List<SiteCustomerListDTO>();

                if (objAPIRequest.cpk != null && objAPIRequest.cpk != string.Empty)
                {
                    try
                    {
                        General.CreateCodeLog("Step 2.6", "Before calling GetSiteCustomerDetailByPrivateKey", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);

                        objCommonBAL = new CommonBAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                        lstCustomerDetailByPrivateKey = objCommonBAL.GetSiteCustomerDetailByPrivateKey(objAPIRequest.cpk, intCountryId, intCurrencyId, intLanguageId);

                        General.CreateCodeLog("Step 2.7", "After calling GetSiteCustomerDetailByPrivateKey", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                    }
                    catch (Exception ex)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                        General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                    }
                }

                if (lstCustomerDetailByPrivateKey != null && lstCustomerDetailByPrivateKey.Count > 0)
                {
                    if (lstCustomerDetailByPrivateKey[0].ExpiryDate > RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes))
                    {
                        if (!string.IsNullOrEmpty(lstCustomerDetailByPrivateKey[0].FirstName))
                        {
                            strLoginCustomerName = lstCustomerDetailByPrivateKey[0].FirstName.Trim();
                        }
                    }
                    else
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Customer_Session_Expired);
                    }
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Customer);
                }
                #endregion
            }
            #endregion

            #region Language Code validations
            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 1.5", "Validating Language Code", "", MethodName);

                if (!string.IsNullOrWhiteSpace(objAPIRequest.LanguageCode))
                {

                    objMasterBAL = new MasterBAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                    dtDataTable = objMasterBAL.GetMasterDataByCodeInternal(General.MainMasterDataCodes.Language.ToString(), objAPIRequest.LanguageCode, lstSiteDetails, lstSiteSolrURlListDTO);

                    if (dtDataTable != null && dtDataTable.Rows.Count > 0 && dtDataTable.Columns.Contains("Id"))
                    {
                        intLanguageId = Convert.ToInt32(dtDataTable.Rows[0]["Id"]);
                    }
                    // intLanguageId = MasterData(General.MainMasterDataCodes.Language.ToString(), strLanguageCode);

                    if (intLanguageId <= 0)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Language_Code);
                    }
                }
            }
            #endregion


            #region GET API Template Details
            if (ErrorCode == 0 && !objAPIRequest.IsStaticApiTemplate)
            {
                if (string.IsNullOrEmpty(objAPIRequest.strProcedure))
                {
                    if (!string.IsNullOrEmpty(objAPIRequest.ModuleCode))
                    {
                        General.CreateCodeLog("Step 2.7", "Before calling GetAPITemplateDetails", "", MethodBase.GetCurrentMethod().Name);

                        APITemplateBAL objAPITemplateBAL = new APITemplateBAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                        lstAPITemplateColumnListDTO = objAPITemplateBAL.GetAPITemplateDetails(null, objAPIRequest.ModuleCode);
                        General.CreateCodeLog("Step 2.8", "After calling GetAPITemplateDetails", "", MethodBase.GetCurrentMethod().Name);

                        if (lstAPITemplateColumnListDTO != null && lstAPITemplateColumnListDTO?.Count >= 0)
                        {
                            objAPIRequest.ModuleId = lstAPITemplateColumnListDTO[0].ModuleId;
                            objAPIRequest.strProcedure = lstAPITemplateColumnListDTO[0].DetailProcedureName;

                            #region Adding Table Names form APi Template Columns
                            lstChildAPITemplateColumn = new List<APITemplateColumnListDTO>();
                            lstChildAPITemplateColumn = lstAPITemplateColumnListDTO.Where(x => x.IsChildTable && x.IsArrayList == false).OrderBy(y => y.ChildTableOrder).ToList();

                            if (lstChildAPITemplateColumn != null && lstChildAPITemplateColumn.Count > 0)
                            {
                                foreach (APITemplateColumnListDTO objChildAPITemplateColumn in lstChildAPITemplateColumn)
                                {
                                    string strChildTableName = objChildAPITemplateColumn.ArrayListName;

                                    if (!lstTableNames.Contains(strChildTableName))
                                    {
                                        lstTableNames.Add(strChildTableName);          //adding table names from childAPITemplateColumn to list variable by doing each iteration through foreach loop
                                    }
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_ModuleCode);
                        }

                    }
                    else
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.ModuleCode_Required);
                    }
                }
            }
            else if (ErrorCode == 0 && objAPIRequest.IsStaticApiTemplate)
            {
                if (string.IsNullOrEmpty(objAPIRequest.strProcedure))
                {
                    if (!string.IsNullOrEmpty(objAPIRequest.ModuleCode))
                    {
                        General.CreateCodeLog("Step 2.7", "Before calling GetAPITemplateDetails", "", MethodBase.GetCurrentMethod().Name);

                        APITemplateBAL objAPITemplateBAL = new APITemplateBAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                        lstStaticAPITemplateColumnListDTO = objAPITemplateBAL.GetStaticAPITemplateDetails(null, objAPIRequest.MethodName, objAPIRequest.ModuleCode);
                        General.CreateCodeLog("Step 2.8", "After calling GetAPITemplateDetails", "", MethodBase.GetCurrentMethod().Name);

                        if (lstStaticAPITemplateColumnListDTO?.Count >= 0)
                        {
                            objAPIRequest.ModuleId = lstStaticAPITemplateColumnListDTO[0].ModuleId;
                            objAPIRequest.strProcedure = lstStaticAPITemplateColumnListDTO[0].StoreporcedureName;

                            if (!string.IsNullOrEmpty(lstStaticAPITemplateColumnListDTO[0].ArrayListitem))
                            {
                                // Split the comma-separated string into an array
                                string[] tableNamesFromDB = lstStaticAPITemplateColumnListDTO[0].ArrayListitem
                                                                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                                if (tableNamesFromDB.Length > 0)
                                {
                                    lstTableNames = new List<string>();

                                    foreach (string tableName in tableNamesFromDB)
                                    {
                                        string trimmedTableName = tableName.Trim();

                                        if (!lstTableNames.Contains(trimmedTableName))
                                        {
                                            lstTableNames.Add(trimmedTableName);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_ModuleCode);
                        }

                    }
                    else
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.ModuleCode_Required);
                    }
                }
            }
            #endregion

            #region Permission Check
            if (ErrorCode == 0 && !IsCustomer)
            {
                objCommonBAL = new CommonBAL(strRespectiveConnectionString, EncryptionKey, objConfigurationSettingsListDTO);
                if (objAPIRequest.ModuleId > 0)
                {
                    try
                    {
                        General.CreateCodeLog("Step 3.0", "Before calling CheckRolePermission", "", MethodBase.GetCurrentMethod().Name);
                        IsAuthorizedUser = objCommonBAL.CheckRolePermission(intRoleId, objAPIRequest.ModuleId, Convert.ToInt32(SAASPOSGeneral.Permissions.View), intCountryId, intCurrencyId, intLanguageId);
                        General.CreateCodeLog("Step 3.1", "After calling CheckRolePermission", "", MethodBase.GetCurrentMethod().Name);
                    }
                    catch (Exception Ex)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                        General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                    }
                    finally
                    {
                        objCommonBAL = null;
                    }
                }

                if (!IsAuthorizedUser)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Do_Not_Have_Permission);
                }
            }
            #endregion Permission Check

            #region GetSiteConfiguration 
            if (ErrorCode == 0)
            {
                if (IsAuthorizedUser)
                {
                    objCommonBAL = new CommonBAL(objConfigurationSettingsListDTO, lstSiteDetails[0].ConnectionString);
                    objApplicationInsights = objCommonBAL.GetSiteConfiguration(Convert.ToInt32(SAASPOSGeneral.SiteConfiguration.IsMicrosoftInsightsRequired), intCountryId, intLanguageId, intCurrencyId);

                    if (objApplicationInsights != null && !string.IsNullOrEmpty(objApplicationInsights.ToString()))
                    {
                        IsMicrosoftInsightsRequired = bool.Parse(objApplicationInsights.ToString());
                    }
                }
            }
            #endregion

            #region Request data Validations

            #region Code Validation
            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 3.2", "Before Validating  the Code", objAPIRequest, MethodBase.GetCurrentMethod().Name, lstSiteDetails);

                if (!string.IsNullOrWhiteSpace(objAPIRequest.Code))
                {
                    strCode = objAPIRequest.Code;
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Code_Required);
                }
                General.CreateCodeLog("Step 3.3", "After Validating  the Code", objAPIRequest, MethodBase.GetCurrentMethod().Name, lstSiteDetails);

            }

            #endregion

            #endregion

            #region Calling DAL
            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 3.4", "Before calling the DAL", objAPIRequest, MethodBase.GetCurrentMethod().Name);
                lstSearchDataListDTO = new List<SearchDataListDTO>();
                if (ErrorCode == 0)
                {

                    objSearchListDTO = new CommonSearchListDTO()
                    {
                        Code = strCode,
                        CountryId = intCountryId,
                        CurrencyId = intCurrencyId,
                        LanguageId = intLanguageId,
                        StoredProcedureName = objAPIRequest.strProcedure,
                    };
                }

                if (objSearchListDTO != null)
                {
                    try
                    {
                        General.CreateCodeLog("Step 3.5", "Starting of GetDataByCodeDB DAL calling - Response", objSearchListDTO, MethodBase.GetCurrentMethod().Name);
                        if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(SAASPOSGeneral.DataBaseTypeId.MsSql))
                        {
                            objSearchDataDAL = new SearchDataDAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                            dsGetSearchData = objSearchDataDAL.GetDataByCodeDB(objSearchListDTO);
                        }
                        else if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(SAASPOSGeneral.DataBaseTypeId.MySql))
                        {
                            objMySqlSearchDataDAL = new MySqlSearchDataDAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                            dsGetSearchData = objMySqlSearchDataDAL.GetDataByCodeDB(objSearchListDTO);
                        }
                        General.CreateCodeLog("Step 3.6", "Ending of GetDataByCodeDB DAL calling - Response", lstSearchDataListDTO, MethodBase.GetCurrentMethod().Name);

                        //Added on 10 Nov 2025
                        #region Getting IsPublished Record if IsCheckAuthenticate is false 

                        if (!objConfigurationSettingsListDTO.IsCheckAuthenticate)
                        {
                            if (dsGetSearchData?.Tables?.Count > 0)
                            {
                                foreach (DataTable table in dsGetSearchData.Tables)
                                {
                                    if (!table.Columns.Contains("IsPublished"))
                                        continue;

                                    // Remove all rows where IsPublished = false
                                    var rowsToRemove = table.AsEnumerable()
                                                            .Where(r => !r.Field<bool>("IsPublished"))
                                                            .ToList();

                                    foreach (var row in rowsToRemove)
                                        table.Rows.Remove(row);

                                }
                            }
                        }

                        #endregion

                        if (dsGetSearchData != null && dsGetSearchData.Tables.Count > 0)
                        {
                            if (lstTableNames != null && lstTableNames.Count == dsGetSearchData.Tables.Count)
                            {
                                for (int i = 0; i < dsGetSearchData.Tables.Count; i++)
                                {
                                    dsGetSearchData.Tables[i].TableName = lstTableNames[i];  //assigning table names from list to object variable.
                                }

                                //if ((objAPIRequest.ModuleId == Convert.ToInt32(SAASPOSGeneral.Module.HRMSEmployeeSalary)) || (objAPIRequest.ModuleId == Convert.ToInt32(SAASPOSGeneral.Module.EmployeeIncomeTax)))
                                //{
                                //    General.DecryptAmounts(dsGetSearchData);

                                //}

                                General.DecryptDataSet(dsGetSearchData, clsCryptography.Decrypt(objConfigurationSettingsListDTO.AESEncryptkey, objConfigurationSettingsListDTO.EncryptionKey),
                                                                                clsCryptography.Decrypt(objConfigurationSettingsListDTO.AESEncryptiv, objConfigurationSettingsListDTO.EncryptionKey));

                                strResponse = JsonConvert.SerializeObject(dsGetSearchData, Formatting.Indented);
                            }
                            else if (lstTableNames != null && dsGetSearchData.Tables.Count > 0 && objAPIRequest.IsStaticApiTemplate)
                            {
                                for (int i = 0; i < lstTableNames.Count; i++)
                                {
                                    dsGetSearchData.Tables[i].TableName = lstTableNames[i];
                                }

                                General.DecryptDataSet(dsGetSearchData, clsCryptography.Decrypt(objConfigurationSettingsListDTO.AESEncryptkey, objConfigurationSettingsListDTO.EncryptionKey),
                                                                                clsCryptography.Decrypt(objConfigurationSettingsListDTO.AESEncryptiv, objConfigurationSettingsListDTO.EncryptionKey));

                                strResponse = JsonConvert.SerializeObject(dsGetSearchData, Formatting.Indented);
                            }
                            else
                            {
                                ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                            }
                        }
                        else
                        {
                            ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.No_Records_Found);
                        }

                        if (IsMicrosoftInsightsRequired)
                        {
                            var dependency = new DependencyTelemetry();
                            var success = dependency.Success.Value;
                            timer.Stop();
                            var telemetryclient = new TelemetryClient();
                            telemetryclient.TrackDependency("Redis", MethodBase.GetCurrentMethod().Name, strResponse, startTime, timer.Elapsed, success);
                        }

                    }
                    catch (Exception Ex)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                        General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                    }
                    finally
                    {
                        objSearchListDTO = null;
                        objSearchDataDAL = null;
                        objMySqlSearchDataDAL = null;
                    }
                }
            }
            #endregion
        }
        else
        {
            ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Missing_Parameters);
        }
    }
    catch (Exception ex)
    {
        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
        General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
    }
    finally
    {
        List<ErrorCodeListDTO> lstErrorCodeListDTO = null;
        try
        {
            if (ErrorCode > 0)
            {
                try
                {
                    objErrorCodeBAL = new ErrorCodeBAL(objConfigurationSettingsListDTO, strRespectiveConnectionString, EncryptionKey);
                    General.CreateCodeLog("Step 3.7", "Satrting of GetErrorCodeById calling - Response", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
                    lstErrorCodeListDTO = objErrorCodeBAL.GetErrorCodeById(objConfigurationSettingsListDTO, ErrorCode, strRespectiveConnectionString, intLanguageId);
                    General.CreateCodeLog("Step 3.8", "Ending of GetErrorCodeById calling - Response", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails);
                }
                catch (Exception Ex)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                    General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                }
                finally
                {
                    objErrorCodeBAL = null;
                }

                if (lstErrorCodeListDTO != null && lstErrorCodeListDTO.Count > 0)
                {
                    objResponse = new Response<object>();
                    objResponse.ReturnCode = lstErrorCodeListDTO[0].ReturnCode;
                    objResponse.ReturnMessage = lstErrorCodeListDTO[0].ReturnMessage;
                    objResponse.Data = null;
                }
            }
            else if (ErrorCode == 0 && dsGetSearchData != null && dsGetSearchData.Tables.Count > 0 && lstTableNames != null && lstTableNames.Count == dsGetSearchData.Tables.Count)
            {
                objResponse = new Response<object>();
                objResponse.ReturnCode = 0;
                objResponse.ReturnMessage = "success";
                objResponse.RecordCount = dsGetSearchData.Tables[lstTableNames[0]].Rows.Count;
                objResponse.ResponseTime = Math.Round((RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes) - startResponseTime).TotalMilliseconds).ToString();
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(strResponse);
                objResponse.Data = jsonResponse;
            }
            else if (ErrorCode == 0 && dsGetSearchData != null && dsGetSearchData.Tables.Count > 0 && objAPIRequest.IsStaticApiTemplate)
            {
                objResponse = new Response<object>();
                objResponse.ReturnCode = 0;
                objResponse.ReturnMessage = "success";
                objResponse.RecordCount = dsGetSearchData.Tables[lstTableNames[0]].Rows.Count;
                objResponse.ResponseTime = Math.Round((RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes) - startResponseTime).TotalMilliseconds).ToString();
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(strResponse);
                objResponse.Data = jsonResponse;
            }

            #region Inserting In APILog 
            objAPILogDetailListDTO = new APILogDetailListDTO();
            objAPILogDetailListDTO.RequestXML = strRequest;
            objAPILogDetailListDTO.ResponseXML = JsonConvert.SerializeObject(objResponse);
            objAPILogDetailListDTO.DateCreated = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes);
            objAPILogDetailListDTO.MethodName = MethodBase.GetCurrentMethod().Name;
            objAPILogDetailListDTO.APIMethodId = Convert.ToInt32(SAASPOSGeneral.APIMethod.GetUserDetailsBySearch);
            objAPILogDetailListDTO.IsRequest = true;
            objAPILogDetailListDTO.ApiLogTypeId = objConfigurationSettingsListDTO.APILogTypeId;
            General.objConfigurationSettingsListDTO = objConfigurationSettingsListDTO;

            if (objAPILogDetailListDTO != null && strRespectiveConnectionString != null && strRespectiveConnectionString != string.Empty)
            {
                try
                {
                    General.CreateCodeLog("Step 3.9", "Satrting of InsertAPILog calling - Response", objAPILogDetailListDTO, MethodBase.GetCurrentMethod().Name);
                    Task tskInsert = Task.Run(() =>
                    {
                        long[] intAPILog = General.InsertAPILog(objAPILogDetailListDTO, strRespectiveConnectionString, objConfigurationSettingsListDTO.EncryptionKey, objConfigurationSettingsListDTO.LogTypeId, objConfigurationSettingsListDTO.CodeLogRequired, lstSiteDetails, lstSiteSolrURlListDTO);
                    });
                    General.CreateCodeLog("Step 4.0", "Ending of InsertAPILog calling - Response", "", MethodBase.GetCurrentMethod().Name);
                }
                catch (Exception Ex)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                    General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                }
                finally
                {
                    objAPILogDetailListDTO = null;
                }
            }
            #endregion

            objResponse.ResponseTime = Math.Round((RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes) - startResponseTime).TotalMilliseconds).ToString();
        }
        catch (Exception ex)
        {
            General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
        }
        finally
        {
            #region Nullifying Objects
            objSearchDataDAL = null;
            lstSearchDataListDTO = null;
            objSearchListDTO = null;
            lstErrorCodeListDTO = null;
            objErrorCodeBAL = null;
            dsGetSearchData = null;
            objMySqlSearchDataDAL = null;
            #endregion
        }

    }
    return objResponse;
}
#endregion
















#region Published
//*********************************************************************************************************
//Purpose            : This method is used to insert/Update Published Details.
//Layer	             :  API
//Method Name        :	Published
//Input Parameters   :  
//Return Values      :  
// --------------------------------------------------------------------------------------------------------
//    Version            Author                     Date               Remarks       
//  -------------------------------------------------------------------------------------------------------
//    1.0	             Srivathsava                26 Sep 2023        Creation
//*********************************************************************************************************
/// <summary>
///This method is used to insert/Update Outlet Details.
/// </summary>
/// <param name=""></param>
/// <returns>ContentResult</returns>
/// 
[HttpPost]
public async Task<ContentResult> Published([FromBody] dynamic objAPIRequest)//SaveRequestDTO objAPIRequest
{
    var HeaderType = Request.ContentType;
    ContentResult objContentResult = null;
    object objResult = null;
    CommonResponse objCommonResponse = null;
    Int32 StatusCode = 0;
    var strClaimsToken = HttpContext.User.Claims;
    var IPAddress = HttpContext.Connection.RemoteIpAddress;
    Response objResponse = null;
    PublishBAL objPublishedBAL = null;
    string strRequest = string.Empty;
    #region After validating request
    try
    {
        strRequest = System.Text.Json.JsonSerializer.Serialize(objAPIRequest);
        Dictionary<string, dynamic> lstRequestObject = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(strRequest);

        objCommonResponse = new CommonResponse
        {
            ReturnCode = CommonResponse.CommonResponseErrorCodes.InvalidRequest,
            ReturnMessage = CommonResponse.dictCommonResponse[CommonResponse.CommonResponseErrorCodes.InvalidRequest.ToString()] //"Invalid Request"
        };


        if (_ConfigurationSettingsListDTO != null && lstRequestObject?.Count > 0)
        {
            //objAPIRequest.ModuleId = Convert.ToInt32(General.Template.Outlet_Module);
            //lstRequestObject.Add("ModuleId", Convert.ToInt32(General.Module.Work_Type));
            var strTokenDetails = strClaimsToken.ToList();

            if (strTokenDetails != null && strTokenDetails.Count > 0)
            {
                lstRequestObject.Add("JwtToken", strTokenDetails.Where(c => c.Type == "stk").Select(c => c.Value).SingleOrDefault());
                lstRequestObject.Add("upk", strTokenDetails.Where(c => c.Type == "upk").Select(c => c.Value).SingleOrDefault());
            }
            General.objConfigurationSettingsListDTO = _ConfigurationSettingsListDTO;
            if (IPAddress != null && IPAddress.ToString() != string.Empty)
            {
                lstRequestObject.Add("IPAddress", IPAddress.ToString());
            }

            General.CreateCodeLog("Step 1", "Published - Before calling of BAL Method", "", MethodBase.GetCurrentMethod().Name);

            Task<Response> tskResponse = Task<Response>.Run(() =>
            {
                objPublishedBAL = new PublishBAL(_ConfigurationSettingsListDTO);
                objResponse = objPublishedBAL.Publish(lstRequestObject);
                return objResponse;
            });

            objResponse = await tskResponse;
            General.CreateCodeLog("Step 2", "Published - End calling of BAL Method", "", MethodBase.GetCurrentMethod().Name);

            if (objResponse != null)
            {
                objResult = objResponse;
            }
            else
            {
                objResult = objCommonResponse;
            }
        }
        else
        {
            objResult = objCommonResponse;
        }
        StatusCode = (int)CommonResponse.CommonResponseErrorCodes.Success;
    }
    catch (Exception ex)
    {
        General.objConfigurationSettingsListDTO = _ConfigurationSettingsListDTO;
        General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name);
        
        StatusCode = (int)CommonResponse.CommonResponseErrorCodes.InvalidRequest;
    }
    finally
    {
        #region Nullifying Objects

        objAPIRequest = null;
        objPublishedBAL = null;

        #endregion
    }
    #endregion

    #region output converting xml or json

    if (HeaderType != null)
    {
        if (HeaderType.ToString().ToLower().Contains("application/xml")) //converting the xml
        {
            objContentResult = new ContentResult() { Content = clsSecurity.ConvertObjectToXml(objResult), ContentType = "application/xml", StatusCode = StatusCode };
        }
        else if (HeaderType.ToString().ToLower().Contains("application/json"))
        {
            objContentResult = new ContentResult() { Content = JsonConvert.SerializeObject(objResult), ContentType = "application/json", StatusCode = StatusCode };
        }
        else
        {
            objContentResult = new ContentResult() { Content = JsonConvert.SerializeObject(objResult), ContentType = "application/json", StatusCode = StatusCode };
        }
    }
    else
    {
        objContentResult = new ContentResult() { Content = JsonConvert.SerializeObject(objResult), ContentType = "application/json", StatusCode = StatusCode };
    }
    return objContentResult;
    #endregion
}
#endregion







#region IsPublished
//***************************************************************************************************
// Layer                        :   BAL 
// Method Name                  :   IsPublished
// Method Description           :   This method is used to Update IsPublished.
// Author                       :   Srivathsava
// Creation Date                :   26 Sep 2023
// Input Parameters             :   objIsPublishedRequestDTO
// Modified Date                : 
// Modified Reason              :
// Return Values                :   INT(1 = Success,0=Fail).
//----------------------------------------------------------------------------------------------------
//  Version             Author                      Date                        Remarks       
// ---------------------------------------------------------------------------------------------------
//  1.0                 Srivathsava                 26 Sep 2021                 Creation
//  1.1                 Prakash T                      03 Oct 2025              RDLC - REV0036/MS/RDLC/865 -Common dll separation changes - Revalsys.SAASPOSCommon Reference Added
//                                                                              and using SAASPOSGeneral for enums and using SAASPOSRegularExpressions.cs for Regex validation
//****************************************************************************************************
/// <summary>
/// <c>IsPublished</c> This method is used to Update IsPublished.
/// <param>objIsPublishedRequestDTO</param>
/// <returns>Response</returns> //It returns the Integer value(1 = Success,0=Fail).
/// </summary>
public Response Publish(Dictionary<string, dynamic> lstRequestObject)
{
    DateTime startTime = DateTime.MinValue; bool IsMicrosoftInsightsRequired = false;
    Stopwatch timer = null; object objApplicationInsights = null;

    startTime = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes);
    timer = System.Diagnostics.Stopwatch.StartNew();

    #region Common  Variables
    SAASPOSRegularExpressions objRegularExpressions = null;
    APILogDetailListDTO objAPILogDetailListDTO = null;
    List<ErrorCodeListDTO> lstErrorCodeListDTO = null;
    CommonBAL objCommonBAL = null;         //Added by Manisha on 25 July 2024 
    ErrorCodeBAL objErrorCodeBAL = null;   //Added by Manisha on 25 July 2024
    TimeZone = "India Standarad Time"; NoOfHours = 5; NoOfMinutes = 30;
    DateTime startResponseTime = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes);
    Response objResponse = null;
    string strFirstName = string.Empty, strCountryCode = string.Empty, strCurrenyCode = string.Empty, strLanguageCode = string.Empty, strIPAddress = string.Empty , strStatusId = string.Empty , strRemarks = string.Empty;
    int intRoleId = 0, intCountryId = 0, intCurrencyId = 0, intLanguageId = 0, ErrorCode = 0;
    MasterBAL objMasterBAL = null;
    DataTable dtDataTable = null;
    #endregion

    #region  Specified variables

    bool IsAuthorizedUser = false, IsErroneous = false;
    string strCashRegisterName = string.Empty, strStoreCode = string.Empty, strRequest = string.Empty;
    List<APITemplateColumnListDTO> lstAPITemplateColumnListDTO = null;
    APITemplateDAL objUploadTemplateDAL = null;
    PublishDAL objPublishDAL = null;
    MySqlPublishDAL objMySqlPublishDAL = null;
    IDictionary<dynamic, dynamic> RequestObject = new Dictionary<dynamic, dynamic>();
    dynamic objRequestObject = new ExpandoObject();
    string strquery = string.Empty, strParameterquery = string.Empty, strFilterIndexErrorDescription = string .Empty, strValueQuery = string.Empty, strMethodName = MethodBase.GetCurrentMethod().Name, strMasterId = string.Empty, strResponse = string.Empty;
    StringBuilder strErrorMessage = new StringBuilder();
    List<string> strHeaders = new List<string>();
    DataTable dtResult = null ;
    IDictionary<dynamic, dynamic> RequestChildObject = new Dictionary<dynamic, dynamic>();
    string strValue = string.Empty, strModuleCode = string.Empty, strImageName = string.Empty, strImagePath = string.Empty, strSaveImagePath = string.Empty; string[] formats = { "M/d/yy, h:mm tt", "M/d/yyyy, h:mm tt", "dd/MM/yyyy", "dd/MM/yy", "d/M/yy", "MM/dd/yyy" };
    bool IsValue = false;
    DateTime dtValue = DateTime.MinValue;
    string strRespectiveConnectionString = string.Empty, strSiteToken = string.Empty;
    DateTime dtTokenExpiryTime = DateTime.MinValue;
    #endregion

    try
    {
        General.CreateCodeLog("Step 1.0", "Before Validating IsPublished Details", "", MethodBase.GetCurrentMethod().Name);

        objRegularExpressions = new SAASPOSRegularExpressions();

        if (lstRequestObject != null)
        {

            #region common validation
            General.CreateCodeLog("Step 1.1", "Before Validating Common Validations", "", MethodBase.GetCurrentMethod().Name);
            #region CountryCode validations

            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 1.2", "Before Validating Countrycode", "", MethodBase.GetCurrentMethod().Name);
                if (lstRequestObject.ContainsKey("CountryCode") && !string.IsNullOrEmpty(lstRequestObject["CountryCode"]))
                {
                    strCountryCode = lstRequestObject["CountryCode"];// objAPIRequest.CountryCode;

                    if (!string.IsNullOrEmpty(strCountryCode))
                    {
                        intCountryId = General.GetCountryId(strCountryCode);
                    }
                    if (intCountryId <= 0)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Country_Code);
                    }
                }
                General.CreateCodeLog("Step 1.3", "After Validating Common Validations", "", MethodBase.GetCurrentMethod().Name);
            }
            #endregion

            #region Curreny Code validations
            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 1.4", "Before Validating CurrencyCode", "", MethodBase.GetCurrentMethod().Name);
                if (lstRequestObject.ContainsKey("CurrencyCode") && !string.IsNullOrEmpty(lstRequestObject["CurrencyCode"]))
                {
                    strCurrenyCode = lstRequestObject["CurrencyCode"];// objAPIRequest.CurrencyCode;

                    if (!string.IsNullOrEmpty(strCurrenyCode))
                    {
                        intCurrencyId = General.GetCurrencyId(strCurrenyCode);
                    }

                    if (intCurrencyId <= 0)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Currency_Code);
                    }
                }
                General.CreateCodeLog("Step 1.5", "After Validating CurrencyCode", "", MethodBase.GetCurrentMethod().Name);
            }
            #endregion

            //#region Language Code validations
            //if (ErrorCode == 0)
            //{
            //    General.CreateCodeLog("Step 1.6", "Before Validating LanguageCode", "", MethodBase.GetCurrentMethod().Name);
            //    if (lstRequestObject.ContainsKey("LanguageCode") && !string.IsNullOrEmpty(lstRequestObject["LanguageCode"]))
            //    {
            //        strLanguageCode = lstRequestObject["LanguageCode"];// objAPIRequest.LanguageCode;

            //        if (!string.IsNullOrEmpty(strLanguageCode))
            //        {
            //            intLanguageId = General.GetCurrencyId(strLanguageCode);
            //        }

            //        if (intLanguageId <= 0)
            //        {
            //            ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Language_Code);
            //        }
            //    }
            //    General.CreateCodeLog("Step 1.7", "After Validating LanguageCode", "", MethodBase.GetCurrentMethod().Name);
            //}
            //#endregion

            #region Assign IP Address

            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 1.8", "Before Assigning the  IPAddress", "", MethodBase.GetCurrentMethod().Name);
                if (lstRequestObject.ContainsKey("IPAddress") && !string.IsNullOrEmpty(lstRequestObject["IPAddress"]))
                {
                    strIPAddress = lstRequestObject["IPAddress"];
                }
                else
                {
                    strIPAddress = General.GetIP4Address();
                    lstRequestObject.Add("IPAddress", strIPAddress);
                    //objAPIRequest.IPAddress = strIPAddress;
                }
                General.CreateCodeLog("Step 1.9", "After Assigning the  IPAddress", "", MethodBase.GetCurrentMethod().Name);
            }
            #endregion
            General.CreateCodeLog("Step 2.0", "After Validating Common Validations", "", MethodBase.GetCurrentMethod().Name);
            #endregion

            //Added by Manikanta Volvoji on 14 Oct 2025
            #region Status and Remarks
            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 1.2", "Before Validating StatusId", "", MethodBase.GetCurrentMethod().Name);

                if (lstRequestObject.ContainsKey("StatusId") && !string.IsNullOrEmpty(lstRequestObject["StatusId"]))
                {
                    strStatusId = lstRequestObject["StatusId"];
                    if (!string.IsNullOrWhiteSpace(strStatusId))
                    {
                        RequestObject.Add("StatusId", strStatusId);
                    }
                }

                General.CreateCodeLog("Step 1.2", "Before Validating StatusId", "", MethodBase.GetCurrentMethod().Name);
            }

            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 1.2", "Before Validating Remarks", "", MethodBase.GetCurrentMethod().Name);

                if (lstRequestObject.ContainsKey("Remarks") && !string.IsNullOrEmpty(lstRequestObject["Remarks"]))
                {
                    strRemarks = lstRequestObject["Remarks"];
                    if (!string.IsNullOrWhiteSpace(strRemarks))
                    {
                        RequestObject.Add("Remarks", strRemarks);
                    }
                }

                General.CreateCodeLog("Step 1.2", "Before Validating Remarks", "", MethodBase.GetCurrentMethod().Name);
            }
            #endregion

            #region validate Token

            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 2.1", "Before getting GetSiteDetails", "", strMethodName,lstSiteDetails, lstSiteSolrURlListDTO);

                lstSiteDetails = new List<SiteDetails>();
                

                if (lstRequestObject.ContainsKey("JwtToken") && !string.IsNullOrEmpty(lstRequestObject["JwtToken"]))
                {
                    objCommonBAL = new CommonBAL(_objConfigurationSettingsListDTO);
                    lstSiteDetails = objCommonBAL.GetSiteDetails(lstRequestObject["JwtToken"], intCountryId, intCurrencyId, intLanguageId);

                }
                if (lstSiteDetails != null && lstSiteDetails.Count > 0)
                {
                    ErrorCode = lstSiteDetails[0].ReturnCode;

                    if (ErrorCode == 0)
                    {
                        strRespectiveConnectionString = lstSiteDetails[0].ConnectionString;
                        RespectiveConnectionString = lstSiteDetails[0].ConnectionString;
                        strSiteToken = lstSiteDetails[0].SecurityToken;
                        dtTokenExpiryTime = lstSiteDetails[0].TokenExpiryTime;
                        SiteCode = lstSiteDetails[0].SiteCode;
                        TimeZone = lstSiteDetails[0].TimeZone;
                        NoOfHours = lstSiteDetails[0].NoOfHours;
                        NoOfMinutes = lstSiteDetails[0].NoOfMinutes;
                    }
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Token);
                }
                General.CreateCodeLog("Step 2.2", "After getting GetSiteDetails", "", strMethodName, lstSiteDetails);
            }
            #endregion

            #region GetUserDetailByPrivateKey

            if (ErrorCode == 0)
            {
                List<SiteUserListDTO> lstSiteDetailByPrivateKey = new List<SiteUserListDTO>();

                if (lstRequestObject.ContainsKey("upk") && !string.IsNullOrEmpty(lstRequestObject["upk"]))
                {
                    try
                    {
                        General.CreateCodeLog("Step 2.3", "Before calling GetSiteUserDetailByPrivateKey", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);

                        objCommonBAL = new CommonBAL(_objConfigurationSettingsListDTO, strRespectiveConnectionString);
                        lstSiteDetailByPrivateKey = objCommonBAL.GetSiteUserDetailByPrivateKey(lstRequestObject["upk"], intCountryId, intCurrencyId, intLanguageId);

                        General.CreateCodeLog("Step 2.4", "After calling GetSiteUserDetailByPrivateKey", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);

                    }
                    catch (Exception ex)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                        General.CreateErrorLog(ex, strMethodName, lstSiteDetails, lstSiteSolrURlListDTO);
                    }
                }

                if (lstSiteDetailByPrivateKey != null && lstSiteDetailByPrivateKey.Count > 0)
                {
                    if (lstSiteDetailByPrivateKey[0].ExpiryDate > RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes))
                    {
                        intRoleId = lstSiteDetailByPrivateKey[0].RoleId;
                        LoginUserName = lstSiteDetailByPrivateKey[0].FirstName;
                    }
                    else
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.User_Session_Expired);
                    }
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_User);
                }
            }
            #endregion GetSiteDetailByPrivateKey

            #region Language Code validations
            if (ErrorCode == 0)
            {
                General.CreateCodeLog("Step 1.75", "After validating the LanguageCode", "", MethodBase.GetCurrentMethod().Name);
                if (!string.IsNullOrEmpty(lstRequestObject["LanguageCode"]))
                {
                    objMasterBAL = new MasterBAL(_objConfigurationSettingsListDTO, RespectiveConnectionString);
                    dtDataTable = objMasterBAL.GetMasterDataByCodeInternal(SAASPOSGeneral.MasterDataCodes.Language.ToString(), lstRequestObject["LanguageCode"], lstSiteDetails, lstSiteSolrURlListDTO);

                    if (dtDataTable != null && dtDataTable.Rows.Count > 0 && dtDataTable.Columns.Contains("Id"))
                    {
                        intLanguageId = Convert.ToInt32(dtDataTable.Rows[0]["Id"]);
                    }
                    // intLanguageId = MasterData(General.MainMasterDataCodes.Language.ToString(), strLanguageCode);
                    else
                    {
                        intLanguageId = General.GetLanguageId(lstRequestObject["LanguageCode"]);
                    }
                    if (intLanguageId <= 0)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Language_Code);
                    }
                }
                General.CreateCodeLog("Step 1.75", "After validating the LanguageCode", "", MethodBase.GetCurrentMethod().Name);
            }
            #endregion


            #region GET API Template Details
            if (ErrorCode == 0)
            {
                if (lstRequestObject.ContainsKey("ModuleCode") && !string.IsNullOrEmpty(lstRequestObject["ModuleCode"]))
                {
                    General.CreateCodeLog("Step 2.5", "Before calling GetAPITemplateDetails", "", strMethodName);
                    APITemplateBAL objAPITemplateBAL = new APITemplateBAL(_objConfigurationSettingsListDTO, RespectiveConnectionString);       //added by charan 29-07-2024 
                    lstAPITemplateColumnListDTO = objAPITemplateBAL.GetAPITemplateDetails(lstRequestObject);
                    General.CreateCodeLog("Step 2.6", "After calling GetAPITemplateDetails", "", strMethodName);

                    if (lstAPITemplateColumnListDTO != null && lstAPITemplateColumnListDTO?.Count >= 0)
                    {
                        intModuleId = lstAPITemplateColumnListDTO[0].ModuleId;
                    }
                    else
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_ModuleCode);
                    }
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.ModuleCode_Required);
                }
            }
            #endregion

            #region Permission Check 

            if (ErrorCode == 0)
            {

                List<RoleListDTO> lsRolePermissionListDTO = new List<RoleListDTO>();
                CommonBAL objRoleBAL = new CommonBAL(RespectiveConnectionString, _objConfigurationSettingsListDTO.EncryptionKey, _objConfigurationSettingsListDTO);

                if (intModuleId > 0)
                {
                    try
                    {
                        General.CreateCodeLog("Step 2.7", "Before calling CheckRolePermission", "", strMethodName);
                        IsAuthorizedUser = objRoleBAL.CheckRolePermission(intRoleId, intModuleId, Convert.ToInt32(SAASPOSGeneral.Permissions.Create_Update), intCountryId, intCurrencyId, intLanguageId);
                        General.CreateCodeLog("Step 2.8", "After calling CheckRolePermission", "", strMethodName);
                    }
                    catch (Exception Ex)
                    {
                        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                        General.CreateErrorLog(Ex, strMethodName, lstSiteDetails, lstSiteSolrURlListDTO);
                    }
                }

                if (!IsAuthorizedUser)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Do_Not_Have_Permission);
                }
            }
            #endregion Permission Check

            #region GetSiteConfiguration 
            if (ErrorCode == 0)
            {
                if (IsAuthorizedUser)
                {
                    objCommonBAL = new CommonBAL(_objConfigurationSettingsListDTO, lstSiteDetails[0].ConnectionString);
                    objApplicationInsights = objCommonBAL.GetSiteConfiguration(Convert.ToInt32(SAASPOSGeneral.SiteConfiguration.IsMicrosoftInsightsRequired), intCountryId, intLanguageId, intCurrencyId);

                    if (objApplicationInsights != null && !string.IsNullOrEmpty(objApplicationInsights.ToString()))
                    {
                        IsMicrosoftInsightsRequired = bool.Parse(objApplicationInsights.ToString());
                    }
                }
            }
            #endregion

            if (ErrorCode == 0 && lstRequestObject?.Count > 0 && lstAPITemplateColumnListDTO?.Count > 0)
            {
                if (lstAPITemplateColumnListDTO?.Count > 0 && ErrorCode == 0)
                {
                    #region IsMandatory
                    
                    foreach (var objAdditionalAPIColumnListDTO in lstAPITemplateColumnListDTO)
                    {
                        #region Static Modules
                        if (objAdditionalAPIColumnListDTO.IsView)
                        {
                            var item = lstRequestObject.Where(x => x.Key.ToLower() == "ISPublished".ToLower()).Select(obj => obj).FirstOrDefault();

                            objAdditionalAPIColumnListDTO.DBColumnName = item.Key;
                            strValue = Convert.ToString(item.Value);

                            if (!string.IsNullOrEmpty(item.Key))
                            {
                                objAdditionalAPIColumnListDTO.DBDataType =  SAASPOSGeneral.DBDataType.Bit.ToString().ToLower();

                                bool.TryParse(strValue, out IsValue);
                                RequestObject.Add(objAdditionalAPIColumnListDTO.DBColumnName, IsValue);
                                

                            }
                            else
                            {
                                if (objAdditionalAPIColumnListDTO.IsMandatoryRequestParameter)
                                {
                                    IsErroneous = true;
                                    strErrorMessage.Append("IsPublished is missing." + ",");
                                }
                            }
                        }
                        #endregion

                        #region Dynamic Modules
                        if (objAdditionalAPIColumnListDTO.APITemplateColumnName == "IsPublished")
                        {
                            var item = lstRequestObject.Where(x => x.Key.ToLower() == objAdditionalAPIColumnListDTO.APITemplateColumnName.ToLower()).Select(obj => obj).FirstOrDefault();
                            
                            strValue = Convert.ToString(item.Value);
                            
                            if (!string.IsNullOrEmpty(item.Key))
                            {
                                if (objAdditionalAPIColumnListDTO.DBDataType.ToLower() == SAASPOSGeneral.DBDataType.Bit.ToString().ToLower())
                                {
                                    bool.TryParse(strValue, out IsValue);
                                    RequestObject.Add(objAdditionalAPIColumnListDTO.DBColumnName, IsValue);
                                }

                            }
                            else
                            {
                                if (objAdditionalAPIColumnListDTO.IsMandatoryRequestParameter)
                                {
                                    IsErroneous = true;
                                    strErrorMessage.Append(objAdditionalAPIColumnListDTO.APITemplateColumnName + " is missing." + ",");
                                }
                            }
                        }
                        #endregion
                    }
                    #endregion

                    #region Calling DAL region

                    if (ErrorCode == 0 && !IsErroneous && RequestObject != null && RequestObject.Count > 0)
                    {

                        try
                        {
                            if (lstRequestObject.ContainsKey(lstAPITemplateColumnListDTO[0].MainTableColumnIdName) && !string.IsNullOrEmpty(lstRequestObject[lstAPITemplateColumnListDTO[0].MainTableColumnIdName]))
                            {
                                RequestObject.Add("Id", lstRequestObject[lstAPITemplateColumnListDTO[0].MainTableColumnIdName]);
                            }
                            else
                            {
                                RequestObject.Add("Id", null);
                            }
                            if (!RequestObject.ContainsKey("IsPublished"))
                            {
                                RequestObject.Add("IsPublished", true);
                            }
                            RequestObject.Add("DatePublished", RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes));
                            RequestObject.Add("PublishedBy", LoginUserName);
                            RequestObject.Add("LastUpdated", RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes));
                            RequestObject.Add("UpdatedBy", LoginUserName);
                            RequestObject.Add("Comments", string.Empty);
                            RequestObject.Add("CountryId", intCountryId);
                            RequestObject.Add("CurrencyId", intCurrencyId);
                            RequestObject.Add("LanguageId", intLanguageId);
                            objUploadTemplateDAL = new APITemplateDAL(_objConfigurationSettingsListDTO, RespectiveConnectionString);
                            General.CreateCodeLog("Step 2.9", "Before calling Publish method", lstAPITemplateColumnListDTO[0].UpdatePublishedProcedureName, strMethodName);
                            if (_objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(SAASPOSGeneral.DataBaseTypeId.MsSql))
                            {
                                if (strRespectiveConnectionString != null && strRespectiveConnectionString != string.Empty)
                                {
                                    objPublishDAL = new PublishDAL(_objConfigurationSettingsListDTO, RespectiveConnectionString);
                                }
                                dtResult = objPublishDAL.Publish(RequestObject, lstAPITemplateColumnListDTO);
                            }
                            else if (_objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(SAASPOSGeneral.DataBaseTypeId.MySql))
                            {
                                if (strRespectiveConnectionString != null && strRespectiveConnectionString != string.Empty)
                                {
                                    objMySqlPublishDAL = new MySqlPublishDAL(_objConfigurationSettingsListDTO, RespectiveConnectionString);
                                }
                                dtResult = objMySqlPublishDAL.Publish(RequestObject, lstAPITemplateColumnListDTO);
                            }
                            General.CreateCodeLog("Step 3.0", "After  calling Publish method", "", strMethodName);

                            if (dtResult.Rows.Count <= 0)
                            {
                                ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Invalid_Code);
                            }
                            else
                            {
                                strResponse = JsonConvert.SerializeObject(dtResult);
                            }
                            if (!string.IsNullOrEmpty(strResponse))
                            {
                                if (IsMicrosoftInsightsRequired)
                                {
                                    var dependency = new DependencyTelemetry();
                                    var success = dependency.Success.Value;
                                    timer.Stop();
                                    var telemetryclient = new TelemetryClient();
                                    telemetryclient.TrackDependency("Redis", MethodBase.GetCurrentMethod().Name, strResponse, startTime, timer.Elapsed, success);
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            General.objConfigurationSettingsListDTO = _objConfigurationSettingsListDTO;
                            ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                            IsErroneous = true;
                            General.CreateErrorLog(ex, strMethodName, lstSiteDetails, lstSiteSolrURlListDTO);

                            #region UNIQUE/FOREIGN/Filter Key error Reading
                            if (ex.Message.Contains("IDX_UNQ_") && !string.IsNullOrWhiteSpace(lstAPITemplateColumnListDTO[0].FilterIndexErrorDescription)) //Added By Subbarao B on 20th jun  
                            {
                                strFilterIndexErrorDescription = lstAPITemplateColumnListDTO[0].FilterIndexErrorDescription;
                                strErrorMessage.Append(strFilterIndexErrorDescription);
                                ErrorCode = -1;

                            }
                            else if (ex.Message.Contains("IDX_UNQ_"))
                            {
                                ErrorCode = -1;
                                string pattern = @"unique index '([^']+)'";

                                Match match = Regex.Match(ex.Message, pattern);
                                if (match.Success)
                                {
                                    if (match.Groups[1].Value.Contains('_'))
                                    {
                                        int lastUnderscoreIndex = match.Groups[1].Value.LastIndexOf('_');
                                        if (lastUnderscoreIndex >= 0 && lastUnderscoreIndex < match.Groups[1].Value.Length - 1)
                                        {
                                            strErrorMessage.Append(match.Groups[1].Value.Substring(lastUnderscoreIndex + 1) + " combination record already exists in system." + ",");
                                        }
                                    }
                                    else
                                    {
                                        strErrorMessage.Append(match.Groups[1].Value + " combination record already exists in system." + ",");
                                    }

                                }
                                else
                                {
                                    strErrorMessage.Append(ex.Message + " combination record already exists in system." + ",");
                                }
                            }
                            else if (ex.Message.Contains("FK_"))
                            {
                                ErrorCode = -1;
                                string strpattern = @"FOREIGN KEY constraint ""([^""]+)""";

                                Match strmatch = Regex.Match(ex.Message, strpattern);
                                if (strmatch.Success)
                                {
                                    if (strmatch.Groups[1].Value.Contains('_'))
                                    {
                                        int lastUnderscoreIndex = strmatch.Groups[1].Value.LastIndexOf('_');
                                        if (lastUnderscoreIndex >= 0 && lastUnderscoreIndex < strmatch.Groups[1].Value.Length - 1)
                                        {
                                            strErrorMessage.Append(strmatch.Groups[1].Value.Substring(lastUnderscoreIndex + 1) + " record not exists in system." + ",");
                                        }
                                    }
                                    else
                                    {
                                        strErrorMessage.Append(strmatch.Groups[1].Value + " record not exists in system." + ",");
                                    }
                                }
                                else
                                {
                                    strErrorMessage.Append(ex.Message + " record not exists in system." + ",");
                                }
                            }
                            #endregion
                        }
                    }
                    #endregion
                }
                else
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.No_Records_Found);
                }
            }

        }
    }
    catch (Exception ex)
    {
        General.objConfigurationSettingsListDTO = _objConfigurationSettingsListDTO;
        ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
        General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
    }
    finally
    {

        #region Error/Success response region
        try
        {
            if (ErrorCode > 0)
            {
                try
                {

                    General.CreateCodeLog("Step 3.1", "Starting of GetErrorCodeById calling - Response", "", MethodBase.GetCurrentMethod().Name);

                    objErrorCodeBAL = new ErrorCodeBAL(_objConfigurationSettingsListDTO, strRespectiveConnectionString, EncryptionKey);
                    lstErrorCodeListDTO = objErrorCodeBAL.GetErrorCodeById(_objConfigurationSettingsListDTO, ErrorCode, strRespectiveConnectionString, intLanguageId);

                    General.CreateCodeLog("Step 3.2", "Ending of GetErrorCodeById calling - Response", "", MethodBase.GetCurrentMethod().Name);
                }
                catch (Exception Ex)
                {
                    ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
                    General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                }
                finally
                {
                    objErrorCodeBAL = null;
                }
                if (lstErrorCodeListDTO != null && lstErrorCodeListDTO.Count > 0)
                {
                    objResponse = new Response();
                    objResponse.ReturnCode = lstErrorCodeListDTO[0].ReturnCode;
                    objResponse.ReturnMessage = lstErrorCodeListDTO[0].ReturnMessage;
                    objResponse.Data = null;
                }
            }
            else if ((ErrorCode == 0 || ErrorCode == -1) && !string.IsNullOrWhiteSpace(strErrorMessage.ToString()))
            {
                objResponse = new Response();
                objResponse.ReturnCode = -1;
                objResponse.ReturnMessage = strErrorMessage.ToString().TrimEnd(',');
                objResponse.Data = null;
            }
            else if (ErrorCode == 0)
            {
                objResponse = new Response();
                dynamic objResponsedetails = new ExpandoObject();
                objResponse.ReturnCode = 0;
                objResponse.ReturnMessage = "success";
                objResponse.ResponseTime = Math.Round((RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes) - startResponseTime).TotalMilliseconds).ToString();
                var json = JsonConvert.DeserializeObject<dynamic>(strResponse);
                objResponse.Data = json;
            }

        }
        catch (Exception ex)
        {
            General.objConfigurationSettingsListDTO = _objConfigurationSettingsListDTO;
            ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
            General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
        }
        #endregion

        #region Inserting In APILog
        try
        {
            objAPILogDetailListDTO = new APILogDetailListDTO();
            objAPILogDetailListDTO.RequestXML = JsonConvert.SerializeObject(lstRequestObject);
            objAPILogDetailListDTO.ResponseXML = JsonConvert.SerializeObject(objResponse);
            objAPILogDetailListDTO.MethodName = MethodBase.GetCurrentMethod().Name;
            objAPILogDetailListDTO.ProgramCode = _objConfigurationSettingsListDTO.ProjectName;
            objAPILogDetailListDTO.CreatedBy = LoginUserName;
            objAPILogDetailListDTO.APIMethodId = Convert.ToInt32(SAASPOSGeneral.APIMethod.Publish);
            objAPILogDetailListDTO.ApiLogTypeId = _objConfigurationSettingsListDTO.APILogTypeId;
            if (objAPILogDetailListDTO != null)
            {
                General.CreateCodeLog("Step 3.3", "Before Calling the  InsertAPILog", "", MethodBase.GetCurrentMethod().Name);
                Task tskInsert = Task.Run(() =>
                {
                    long[] intAPILog = General.InsertAPILog(objAPILogDetailListDTO, RespectiveConnectionString, _objConfigurationSettingsListDTO.EncryptionKey, _objConfigurationSettingsListDTO.LogTypeId, _objConfigurationSettingsListDTO.CodeLogRequired, lstSiteDetails, lstSiteSolrURlListDTO);

                });
                General.CreateCodeLog("Step 3.4", "After Calling the  InsertAPILog", "", MethodBase.GetCurrentMethod().Name);
            }
        }
        catch (Exception ex)
        {
           General.objConfigurationSettingsListDTO = _objConfigurationSettingsListDTO;
           ErrorCode = Convert.ToInt32(SAASPOSGeneral.ErrorCode.Technical_Error_occured);
           General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
        }
        #endregion

        #region Nullfuing objects
        lstSiteDetails = null;
        lstSiteSolrURlListDTO = null;
        objRegularExpressions = null;
        objAPILogDetailListDTO = null;
        lstErrorCodeListDTO = null;
        objCommonBAL = null;
        #endregion
    }

    return objResponse;
}
#endregion






















using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Revalsys.Common;
using Revalsys.Master.BusinessLogic;
using Revalsys.Properties;
using Revalsys.Security;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RevalsysSAASPOSWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [JwtAuthentication]
    public class GetMastersController : ControllerBase
    {

        private ConfigurationSettingsListDTO _ConfigurationSettingsListDTO = null;

        public GetMastersController(IOptions<ConfigurationSettingsListDTO> options)
        {
            _ConfigurationSettingsListDTO = options.Value;
        }

        #region GetMasters
        //*********************************************************************************************************
        //Purpose            :  This API Method will Get Masters tables Data.
        //Layer	             :  API
        //Method Name        :	GetMasters
        //Input Parameters   :  
        //Return Values      :  
        // --------------------------------------------------------------------------------------------------------
        //    Version            Author                     Date               Remarks       
        //  -------------------------------------------------------------------------------------------------------
        //    1.0	             Upendra G               01 Aug 2023        Creation
        //*********************************************************************************************************
        /// <summary>
        /// This API Method will Get Masters tables Data.
        /// </summary>
        /// <param name=""></param>
        /// <returns>ContentResult</returns>
        /// 
        [HttpPost]
        public async Task<ContentResult> GetMasterByCode(MasterListDTO objAPIRequest)
        {
            var HeaderType = Request.ContentType;
            MasterBAL objMasterBAL = null;
            ContentResult objContentResult = null;
            Response<object> objMasterListDTO = null;
            object objResult = null;
            InvalidRequest objInvalidRequest = new InvalidRequest();
            objInvalidRequest.ReturnCode = ((int)CommonResponse.CommonResponseErrorCodes.InvalidRequest).ToString();
            objInvalidRequest.ReturnMessage = Enum.GetName(typeof(CommonResponse.CommonResponseErrorCodes), CommonResponse.CommonResponseErrorCodes.InvalidRequest);
            Int32 StatusCode = 0;
            var strClaimsToken = HttpContext.User.Claims;
            string strupk = string.Empty;
            #region After validating request
            try
            {
                if (_ConfigurationSettingsListDTO != null)
                {
                    var strTokenDetails = strClaimsToken.ToList();
                    if (strTokenDetails != null && strTokenDetails.Count > 0)
                    {
                        General.CreateCodeLog("Step 1", "Starting of Token Assigning calling - Response", "", MethodBase.GetCurrentMethod().Name);
                        objAPIRequest.JwtToken = strTokenDetails.Where(c => c.Type == "stk").Select(c => c.Value).SingleOrDefault(); // strTokenDetails[1].Value.ToString();
                        objAPIRequest.upk = strTokenDetails.Where(c => c.Type == "upk").Select(c => c.Value).SingleOrDefault();
                        General.CreateCodeLog("Step 1.A", "Ending of Token Assigning calling - Response", objAPIRequest, MethodBase.GetCurrentMethod().Name);
                        // objAPIRequest.upk = strTokenDetails[3].Value.ToString();
                    }

                    General.objConfigurationSettingsListDTO = _ConfigurationSettingsListDTO;

                    Task<Response<object>> tskResponse = Task<Response<object>>.Run(() =>
                    {
                        General.CreateCodeLog("Step 1.0", "Starting of GetUserDetailsBySearch BAL calling - Response", "", MethodBase.GetCurrentMethod().Name);
                        objMasterBAL = new MasterBAL(_ConfigurationSettingsListDTO);
                        objMasterListDTO = objMasterBAL.GetMasterByCode(objAPIRequest);
                        General.CreateCodeLog("Step 2", "Ending of GetUserDetailsBySearch BAL calling - Response", "", MethodBase.GetCurrentMethod().Name);
                        return objMasterListDTO;
                    });
                    objMasterListDTO = await tskResponse;

                    if (objMasterListDTO != null)
                    {
                        objResult = objMasterListDTO;
                    }
                    else
                    {
                        objResult = objInvalidRequest;
                    }
                }
                else
                {
                    objResult = objInvalidRequest;
                }
                StatusCode = (int)CommonResponse.CommonResponseErrorCodes.Success;
            }
            catch (Exception ex)
            {
                if (_ConfigurationSettingsListDTO != null && _ConfigurationSettingsListDTO.LogTypeId > 0)
                {
                    General.objConfigurationSettingsListDTO = _ConfigurationSettingsListDTO;                   
                }
                General.CreateCodeLog("Step 2.1", "Exception In GetMaster API" + ex.Message,"", MethodBase.GetCurrentMethod().Name);
                StatusCode = (int)CommonResponse.CommonResponseErrorCodes.BadRequest;
            }
            finally
            {
                #region Nullifying Objects
                objMasterBAL = null;
                objInvalidRequest = null;
                #endregion
            }
            #endregion

            #region output converting xml or json
            if (HeaderType != null)
            {
                if (HeaderType.ToString().ToLower().Contains("application/xml")) //converting the xml
                {
                    objContentResult = new ContentResult() { Content = clsSecurity.ConvertObjectToXml(objResult), ContentType = "application/xml", StatusCode = StatusCode };
                }
                else if (HeaderType.ToString().ToLower().Contains("application/json"))
                {
                    objContentResult = new ContentResult() { Content = JsonConvert.SerializeObject(objResult), ContentType = "application/json", StatusCode = StatusCode };
                }
                else
                {
                    objContentResult = new ContentResult() { Content = JsonConvert.SerializeObject(objResult), ContentType = "application/json", StatusCode = StatusCode };
                }
            }
            else
            {
                objContentResult = new ContentResult() { Content = JsonConvert.SerializeObject(objResult), ContentType = "application/json", StatusCode = StatusCode };
            }
            return objContentResult;
            #endregion
        }
        #endregion
    }
}











        #region GetMasterByCode
        //*********************************************************************************************************
        //Purpose            :  This API Method will Get Master By Code.
        //Layer	             :  API
        //Method Name        :	GetMasterByCode
        //Input Parameters   :  
        //Return Values      :  
        // --------------------------------------------------------------------------------------------------------
        //    Version            Author                     Date               Remarks       
        //  -------------------------------------------------------------------------------------------------------
        //    1.0	             Upendra G               01 Aug 2023        Creation
        //    1.1                Anusha N                26 Mar 2024        Assign struserid from 1stSiteUserDeatils AND added if condition to check masterdatacode
        //    1.1                Nagaraju K              11 Oct 2025        Based on the MasterDataCode, retrieve the respective stored procedure name from the tblMasterData table
        //*********************************************************************************************************
        /// <summary>
        /// This API Method will Get Master By Code.
        /// </summary>
        /// <param name=""></param>
        /// <returns>ContentResult</returns>
        /// 
        public Response<object> GetMasterByCode(MasterListDTO objAPIRequest)
        {
            #region Common Variables
            DateTime startResponseTime = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes);
            int ErrorCode = 0;
            int intRoleId = 0;
            APILogDetailListDTO objAPILogDetailListDTO = null;
            List<SiteDetails> lstSiteDetails = null;
            string strRespectiveConnectionString = string.Empty;
            string strSiteToken = string.Empty;
            DateTime dtTokenExpiryTime = DateTime.MinValue, dtExpiryTime = DateTime.MinValue;
            string strUserPrivateKey = string.Empty;
            List<SiteUserListDTO> lsSiteUserDetailByPrivateKey = null;
            TimeZone = "India Standard Time"; NoOfHours = 5; NoOfMinutes = 30;
            #endregion

            #region Specific Variables
            Response<object> objResponse = null;
            List<MasterDataListDTO> lstMasterDataListDTO = null;
            RegularExpressions objRegularExpressions = null;
            List<MasterListDTO> lstMasterResposneListDTO = null;
            List<SiteSolrURlListDTO> lstSiteSolrURlListDTO = null;
            JwtAuthenticationAttribute objJwtAuthenticationAttribute = null;
            string strUserId = string.Empty, strSiteCode = string.Empty, strLoginUserName = string.Empty;
            string strCountryCode = string.Empty, strCurrenyCode = string.Empty, strLanguageCode = string.Empty, strIPAddress = string.Empty, strRequest = string.Empty, strUserIdCode = string.Empty; 
            int intCountryId = 0, intCurrencyId = 0, intLanguageId = 1;
            SiteDetailListDTO objSiteDetailListDTO = new SiteDetailListDTO();
            string strMasterDataCode = string.Empty, strResponse = string.Empty;
            bool IsActive = false;
            MasterRequestListDTO objMasterRequestListDTO = null;
            DataTable dtGetSearchData = null;
            string strStoreCode = string.Empty, strCode = string.Empty;
            string strSearchWord = string.Empty;
            CommonBAL objCommonBAL = null;         //Added by Manisha on 24 July 2024 

            DataTable dtDataTable = null;
            MasterBAL objMasterBAL = null;
            string strMasterCodeSpName =string.Empty;
            #endregion

            try
            {
                General.objConfigurationSettingsListDTO = objConfigurationSettingsListDTO;
                General.CreateCodeLog("Step 1.0", "Before calling the Method", objAPIRequest, MethodBase.GetCurrentMethod().Name);
                
                if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MsSql))
                {
                    CommonDAL objCommonDAL = new CommonDAL(objConfigurationSettingsListDTO, string.Empty);
                }
                else if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MySql))
                {
                    MySqlCommonDAL objCommonDAL = new MySqlCommonDAL(objConfigurationSettingsListDTO, string.Empty);
                }
                objRegularExpressions = new RegularExpressions();
                strRequest = JsonConvert.SerializeObject(objAPIRequest);

                #region Checking Validations 
                General.CreateCodeLog("Step 1.1", "Before Checking Validations", objAPIRequest, MethodBase.GetCurrentMethod().Name);
                #region CountryCode
                if (ErrorCode == 0)
                {
                    General.CreateCodeLog("Step 1.2", "Before Validating  CountryCode", objAPIRequest, MethodBase.GetCurrentMethod().Name);

                    if (!string.IsNullOrEmpty(objAPIRequest.CountryCode))
                    {
                        strCountryCode = objAPIRequest.CountryCode;

                        if (!string.IsNullOrEmpty(strCountryCode))
                        {
                            intCountryId = General.GetCountryId(strCountryCode);
                        }
                        if (intCountryId <= 0)
                        {
                            ErrorCode = Convert.ToInt32(General.ErrorCode.Invalid_Country_Code);
                        }
                    }
                    General.CreateCodeLog("Step 1.3", "After Validating  CountryCode", objAPIRequest, MethodBase.GetCurrentMethod().Name);
                }
                #endregion

                #region CurrenyCode
                if (ErrorCode == 0)
                {
                    General.CreateCodeLog("Step 1.4", "Before Validating  CurrencyCode", objAPIRequest, MethodBase.GetCurrentMethod().Name);
                    if (!string.IsNullOrEmpty(objAPIRequest.CurrenyCode))
                    {
                        strCurrenyCode = objAPIRequest.CurrenyCode;

                        if (!string.IsNullOrEmpty(strCurrenyCode))
                        {
                            intCurrencyId = General.GetCurrencyId(strCurrenyCode);
                        }

                        if (intCurrencyId <= 0)
                        {
                            ErrorCode = Convert.ToInt32(General.ErrorCode.Invalid_Currency_Code);
                        }
                    }
                    General.CreateCodeLog("Step 1.5", "After Validating  CurrencyCode", objAPIRequest, MethodBase.GetCurrentMethod().Name);
                }
                #endregion

                //#region LanguageCode
                //if (ErrorCode == 0)
                //{
                //    General.CreateCodeLog("Step 1.6", "Before Validating  LanguageCode", objAPIRequest, MethodBase.GetCurrentMethod().Name);
                //    if (!string.IsNullOrEmpty(objAPIRequest.LanguageCode))
                //    {
                //        strLanguageCode = objAPIRequest.LanguageCode;

                //        if (!string.IsNullOrEmpty(strLanguageCode))
                //        {
                //            intLanguageId = General.GetCurrencyId(strLanguageCode);
                //        }

                //        if (intLanguageId <= 0)
                //        {
                //            ErrorCode = Convert.ToInt32(General.ErrorCode.Invalid_Language_Code);
                //        }
                //    }
                //    General.CreateCodeLog("Step 1.7", "After Validating  LanguageCode", objAPIRequest, MethodBase.GetCurrentMethod().Name);
                //}
                //#endregion                              

                #region Assign IP Address
                if (ErrorCode == 0)
                {
                    General.CreateCodeLog("Step 1.6", "Before Assigning the IPAddress", objAPIRequest, MethodBase.GetCurrentMethod().Name);
                    if (!string.IsNullOrEmpty(objAPIRequest.IPAddress))
                    {
                        strIPAddress = objAPIRequest.IPAddress;
                    }
                    else
                    {
                        strIPAddress = General.GetIP4Address();
                        objAPIRequest.IPAddress = strIPAddress;
                    }
                    General.CreateCodeLog("Step 1.7", "After Assigning the IPAddress", objAPIRequest, MethodBase.GetCurrentMethod().Name);
                }

                #endregion

                #region Assign IsActive

                if (ErrorCode == 0)
                {
                    General.CreateCodeLog("Step 1.8", "Before Assigning the Active", objAPIRequest, MethodBase.GetCurrentMethod().Name);
                    if (!string.IsNullOrEmpty(objAPIRequest.Active))
                    {
                        bool.TryParse(objAPIRequest.Active, out IsActive);
                    }
                    General.CreateCodeLog("Step 1.9", "After Assigning the Active", objAPIRequest, MethodBase.GetCurrentMethod().Name);
                }

                #endregion
                General.CreateCodeLog("Step 2.0", "After Checking Validations", objAPIRequest, MethodBase.GetCurrentMethod().Name);

                #endregion Checking Validations

                objJwtAuthenticationAttribute = new JwtAuthenticationAttribute();
                if (objAPIRequest != null)
                {
                    #region ValidateToken 
                    if (ErrorCode == 0)
                    {

                        objJwtAuthenticationAttribute = new JwtAuthenticationAttribute();
                        try
                        {
                            General.CreateCodeLog("Step 2.1", "Before Calling of ValidateToken", "GetMasterByCode");
                            
                            if (!string.IsNullOrEmpty(objAPIRequest.JwtToken))
                            {
                                General.CreateCodeLog("Step 2.2", "Before calling GetSiteDetails", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);

                                objCommonBAL = new CommonBAL(objConfigurationSettingsListDTO);
                                lstSiteDetails = objCommonBAL.GetSiteDetails(objAPIRequest.JwtToken, intCountryId, intCurrencyId, intLanguageId);

                                General.CreateCodeLog("Step 2.3", "After calling GetSiteDetails", "", MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);

                            }
                            General.CreateCodeLog("Step 2.4", "After Calling of ValidateToken", "GetMasterByCode");
                        }
                        catch (Exception Ex)
                        {
                            ErrorCode = Convert.ToInt32(General.ErrorCode.Technical_Error_occured);
                            General.objConfigurationSettingsListDTO = objConfigurationSettingsListDTO;
                            General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                            
                            General.CreateCodeLog("Step 2.5", "Request Validation - Request", ErrorCode.ToString());
                        }

                        if (lstSiteDetails != null && lstSiteDetails.Count > 0)
                        {
                            ErrorCode = lstSiteDetails[0].ReturnCode;
                            General.CreateCodeLog("Step 2.6", "Request Validation - Request", ErrorCode.ToString());
                            if (ErrorCode == 0)
                            {
                                strRespectiveConnectionString = lstSiteDetails[0].ConnectionString;
                                strSiteToken = lstSiteDetails[0].SecurityToken;
                                dtTokenExpiryTime = lstSiteDetails[0].TokenExpiryTime;
                                strUserPrivateKey = lstSiteDetails[0].UserPrivateKey;
                                SiteCode = lstSiteDetails[0].SiteCode;
                                TimeZone = lstSiteDetails[0].TimeZone;
                                NoOfHours = lstSiteDetails[0].NoOfHours;
                                NoOfMinutes = lstSiteDetails[0].NoOfMinutes;                               
                            }
                        }
                        else
                        {
                            ErrorCode = Convert.ToInt32(General.ErrorCode.Invalid_Token);
                        }
                    }
                    #endregion

                    #region GetSiteUserDetailByPrivateKey
                    if (ErrorCode == 0)
                    {
                        General.CreateCodeLog("Step 2.7", "Before calling UserDAL", "", MethodBase.GetCurrentMethod().Name);
                        lsSiteUserDetailByPrivateKey = new List<SiteUserListDTO>();
                        
                        if (objAPIRequest.upk != null && objAPIRequest.upk != string.Empty)
                        {
                            try
                            {
                                General.CreateCodeLog("Step 2.8", "Before calling GetSiteUserDetailByPrivateKey", "", MethodBase.GetCurrentMethod().Name);

                                objCommonBAL = new CommonBAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                                lsSiteUserDetailByPrivateKey = objCommonBAL.GetSiteUserDetailByPrivateKey(objAPIRequest.upk, intCountryId, intCurrencyId, intLanguageId);

                                General.CreateCodeLog("Step 2.9", "After calling GetSiteUserDetailByPrivateKe", "", MethodBase.GetCurrentMethod().Name);
                            }
                            catch (Exception ex)
                            {
                                ErrorCode = Convert.ToInt32(General.ErrorCode.Technical_Error_occured);
                
                                 General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                                
                            }

                            if (lsSiteUserDetailByPrivateKey != null && lsSiteUserDetailByPrivateKey.Count > 0)
                            {
                                if (lsSiteUserDetailByPrivateKey[0].ExpiryDate > RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes))
                                {
                                    intRoleId = lsSiteUserDetailByPrivateKey[0].RoleId;
                                    strLoginUserName = lsSiteUserDetailByPrivateKey[0].UserName;
                                    strUserId = lsSiteUserDetailByPrivateKey[0].UserGuId;
                                }
                                else
                                {
                                    ErrorCode = Convert.ToInt32(General.ErrorCode.User_Session_Expired);
                                }
                            }
                            else
                            {
                                ErrorCode = Convert.ToInt32(General.ErrorCode.Invalid_User);
                            }
                        }
                    }
                    #endregion GetSiteDetailByPrivateKey

                    #region Language Code validations
                    if (ErrorCode == 0)
                    {
                        General.CreateCodeLog("Step 1.75", "After validating the LanguageCode", "", MethodBase.GetCurrentMethod().Name);
                        if (!string.IsNullOrEmpty(objAPIRequest.LanguageCode))
                        {
                            objMasterBAL = new MasterBAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                            dtDataTable = objMasterBAL.GetMasterDataByCodeInternal(General.MasterDataCodes.Language.ToString(), objAPIRequest.LanguageCode, lstSiteDetails, lstSiteSolrURlListDTO);

                            if (dtDataTable != null && dtDataTable.Rows.Count > 0 && dtDataTable.Columns.Contains("Id"))
                            {
                                intLanguageId = Convert.ToInt32(dtDataTable.Rows[0]["Id"]);
                            }
                            // intLanguageId = MasterData(General.MainMasterDataCodes.Language.ToString(), strLanguageCode);
                            else
                            {
                                intLanguageId = General.GetLanguageId(strLanguageCode);
                            }
                            if (intLanguageId <= 0)
                            {
                                ErrorCode = Convert.ToInt32(General.ErrorCode.Invalid_Language_Code);
                            }
                        }
                        General.CreateCodeLog("Step 1.75", "After validating the LanguageCode", "", MethodBase.GetCurrentMethod().Name);
                    }
                    #endregion


                    #region Validate Request
                    if (ErrorCode == 0)
                    {
                        General.CreateCodeLog("Step 3.0", "Before Validating the StoreCode", "", MethodBase.GetCurrentMethod().Name);
                        if (!string.IsNullOrEmpty(objAPIRequest.StoreCode))
                        {
                            strStoreCode = objAPIRequest.StoreCode.Trim();
                        }
                        General.CreateCodeLog("Step 3.1", "After Validating the StoreCode", "", MethodBase.GetCurrentMethod().Name);
                    }

                    if (ErrorCode == 0)
                    {
                        General.CreateCodeLog("Step 3.2", "Before Validating the Code", "", MethodBase.GetCurrentMethod().Name);
                        if (!string.IsNullOrEmpty(objAPIRequest.Code))
                        {
                            strCode = objAPIRequest.Code.Trim();
                        }
                        General.CreateCodeLog("Step 3.3", "After Validating the Code", "", MethodBase.GetCurrentMethod().Name);
                    }

                    if (ErrorCode == 0)
                    {
                        General.CreateCodeLog("Step 3.4", "Before Validating the SearchWord", "", MethodBase.GetCurrentMethod().Name);
                        if (!string.IsNullOrEmpty(objAPIRequest.SearchWord))
                        {
                            strSearchWord = objAPIRequest.SearchWord.Trim();
                        }
                        General.CreateCodeLog("Step 3.5", "After Validating the SearchWord", "", MethodBase.GetCurrentMethod().Name);
                    }
                    if (ErrorCode == 0)
                    {
                        if (objAPIRequest.MasterDataCode == Convert.ToString(General.MasterDataCodes.UserStore) || objAPIRequest.MasterDataCode == Convert.ToString(General.MasterDataCodes.SaleStore) || objAPIRequest.MasterDataCode == Convert.ToString(General.MasterDataCodes.HRMSEmployeeTask) || objAPIRequest.MasterDataCode == Convert.ToString(General.MasterDataCodes.HRMSEmployeeClientProject))
                        {
                            strUserIdCode = strUserId;
                        }

                    }
                    #endregion

                    #region Calling DAL
                    if (ErrorCode == 0)
                    {
                        if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MsSql))
                        {
                            MasterDAL objMastersDAL = new MasterDAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                        }
                        else if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MySql))
                        {
                            MySqlMasterDAL objMastersDAL = new MySqlMasterDAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                        }
                        
                        if (ErrorCode == 0)
                        {
                            int intPageNumber = 0;
                            int intPageSize = 0;
                            int.TryParse(objAPIRequest.PageNumber, out intPageNumber);
                            int.TryParse(objAPIRequest.PageSize, out intPageSize);
                            if (intPageNumber <= 0 || intPageSize <= 0)
                            {
                                intPageNumber = 1;
                                intPageSize = 999999999;
                            }
                            objMasterRequestListDTO = new MasterRequestListDTO()
                            {
                                PageNumber = intPageNumber,
                                PageSize = intPageSize,
                                MasterDataCode = objAPIRequest.MasterDataCode,
                                IsActive = IsActive,
                                CountryId = intCountryId,
                                CurrencyId = intCurrencyId,
                                LanguageId = intLanguageId,
                                StoreCode = strStoreCode,
                                SearchWord = strSearchWord,
                                Code = strCode,
                                UserId = strUserId
                            };
                            
                        }

                        if (objAPIRequest != null)
                        {
                            MasterDAL objMasterDAL = null;
                            MySqlMasterDAL objMySqlMasterDAL = null;
                            try
                            {

                                if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MsSql))
                                {
                                    objMasterDAL = new MasterDAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                                    strMasterCodeSpName = objMasterDAL.GetSpNameByCode(objMasterRequestListDTO);
                                }
                                else
                                {
                                    objMySqlMasterDAL = new MySqlMasterDAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                                    strMasterCodeSpName = objMySqlMasterDAL.GetSpNameByCode(objMasterRequestListDTO);
                                }                                
                                
                                if(!string.IsNullOrEmpty(strMasterCodeSpName))
                                {
                                    if (intLanguageId == 1)
                                    {
                                        objMasterRequestListDTO.StoredProcedureName = strMasterCodeSpName;
                                        if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MsSql))
                                        {
                                            objMasterDAL = new MasterDAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                                            dtGetSearchData = objMasterDAL.GetMasterDataByCode(objMasterRequestListDTO);
                                        }
                                        else if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MySql))
                                        {
                                            objMySqlMasterDAL = new MySqlMasterDAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                                            dtGetSearchData = objMySqlMasterDAL.GetMasterDataByCode(objMasterRequestListDTO);
                                        }

                                    }
                                    else if (intLanguageId >= 1)
                                    {
                                        if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MsSql))
                                        {
                                            objMasterDAL = new MasterDAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                                            dtGetSearchData = objMasterDAL.GetMasterByLanguageCode(objMasterRequestListDTO);
                                        }
                                        else if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MySql))
                                        {
                                            objMySqlMasterDAL = new MySqlMasterDAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                                            dtGetSearchData = objMySqlMasterDAL.GetMasterByLanguageCode(objMasterRequestListDTO);
                                        }
                                    }
                                    else
                                    {
                                        ErrorCode = Convert.ToInt32(General.ErrorCode.Invalid_Language);
                                    }
                                    
                                    if (dtGetSearchData != null && dtGetSearchData.Rows.Count > 0)
                                    {
                                        General.DecryptDataTable(dtGetSearchData, clsCryptography.Decrypt(objConfigurationSettingsListDTO.AESEncryptkey, objConfigurationSettingsListDTO.EncryptionKey),
                                                                                        clsCryptography.Decrypt(objConfigurationSettingsListDTO.AESEncryptiv, objConfigurationSettingsListDTO.EncryptionKey));

                                        dynamic dyJObjects = dtGetSearchData.AsEnumerable().Cast<dynamic>().ToList().ElementAt(0);
                                        strResponse = JsonConvert.SerializeObject(dyJObjects.Table);
                                    }
                                    else
                                    {
                                        ErrorCode = Convert.ToInt32(General.ErrorCode.No_Records_Found);
                                    }
                                }
                                else
                                {
                                    General.CreateCodeLog("Step 3.6", "Starting of GetMasterByCode DAL calling - Response", objAPIRequest, MethodBase.GetCurrentMethod().Name);

                                    if (intLanguageId == 1)
                                    {
                                        if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MsSql))
                                        {
                                            objMasterDAL = new MasterDAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                                            dtGetSearchData = objMasterDAL.GetMasterByCode(objMasterRequestListDTO);
                                        }
                                        else if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MySql))
                                        {
                                            objMySqlMasterDAL = new MySqlMasterDAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                                            dtGetSearchData = objMySqlMasterDAL.GetMasterByCode(objMasterRequestListDTO);
                                        }
                                    }
                                    else if (intLanguageId >= 1)
                                    {
                                        if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MsSql))
                                        {
                                            objMasterDAL = new MasterDAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                                            dtGetSearchData = objMasterDAL.GetMasterByLanguageCode(objMasterRequestListDTO);
                                        }
                                        else if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MySql))
                                        {
                                            objMySqlMasterDAL = new MySqlMasterDAL(objConfigurationSettingsListDTO, strRespectiveConnectionString);
                                            dtGetSearchData = objMySqlMasterDAL.GetMasterByLanguageCode(objMasterRequestListDTO);
                                        }
                                    }
                                    else
                                    {
                                        ErrorCode = Convert.ToInt32(General.ErrorCode.Invalid_Language);
                                    }

                                    General.CreateCodeLog("Step 3.7", "Ending of GetMasterByCode DAL calling - Response", lstMasterDataListDTO, MethodBase.GetCurrentMethod().Name);

                                    if (dtGetSearchData != null && dtGetSearchData.Rows.Count > 0)
                                    {
                                        General.DecryptDataTable(dtGetSearchData, clsCryptography.Decrypt(objConfigurationSettingsListDTO.AESEncryptkey, objConfigurationSettingsListDTO.EncryptionKey),
                                                                                        clsCryptography.Decrypt(objConfigurationSettingsListDTO.AESEncryptiv, objConfigurationSettingsListDTO.EncryptionKey));

                                        dynamic dyJObjects = dtGetSearchData.AsEnumerable().Cast<dynamic>().ToList().ElementAt(0);
                                        strResponse = JsonConvert.SerializeObject(dyJObjects.Table);
                                    }
                                    else
                                    {
                                        ErrorCode = Convert.ToInt32(General.ErrorCode.No_Records_Found);
                                    }
                                }
                               
                            }
                            catch (Exception Ex)
                            {
                                ErrorCode = Convert.ToInt32(General.ErrorCode.Technical_Error_occured);
                
                                General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                                
                            }
                            finally
                            {
                                objMasterDAL = null;
                                objMySqlMasterDAL = null;
                            }
                        }
                        General.CreateCodeLog("Step 3.8", "Ending of GetMasterByCode DAL calling - Response", lstMasterResposneListDTO, MethodBase.GetCurrentMethod().Name);

                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                General.objConfigurationSettingsListDTO = objConfigurationSettingsListDTO;
                ErrorCode = Convert.ToInt32(General.ErrorCode.Technical_Error_occured);
                
                 General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                
            }
            finally
            {
                ErrorCodeBAL objErrorCodeBAL = null;
                List<ErrorCodeListDTO> lstErrorCodeListDTO = null;
                try
                {
                    if (ErrorCode > 0)
                    {
                        try
                        {
                            objErrorCodeBAL = new ErrorCodeBAL(objConfigurationSettingsListDTO, strRespectiveConnectionString, EncryptionKey);
                            General.CreateCodeLog("Step 3.9", "Satrting of GetErrorCodeById calling - Response", "", MethodBase.GetCurrentMethod().Name);
                            lstErrorCodeListDTO = objErrorCodeBAL.GetErrorCodeById(objConfigurationSettingsListDTO, ErrorCode, strRespectiveConnectionString, intLanguageId);
                            General.CreateCodeLog("Step 4.0", "Ending of GetErrorCodeById calling - Response", "", MethodBase.GetCurrentMethod().Name);
                        }
                        catch (Exception Ex)
                        {
                            ErrorCode = Convert.ToInt32(General.ErrorCode.Technical_Error_occured);
            
                            General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                            
                        }

                        if (lstErrorCodeListDTO != null && lstErrorCodeListDTO.Count > 0)
                        {
                            objResponse = new Response<object>();
                            objResponse.ReturnCode = lstErrorCodeListDTO[0].ReturnCode;
                            objResponse.ReturnMessage = lstErrorCodeListDTO[0].ReturnMessage;
                            objResponse.Data = null;
                        }
                    }
                    else if (ErrorCode == 0)
                    {
                        objResponse = new Response<object>();
                        objResponse.ReturnCode = 0;
                        objResponse.ReturnMessage = "success";
                        objResponse.RecordCount = dtGetSearchData.Rows.Count;
                        objResponse.ResponseTime = Math.Round((RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes) - startResponseTime).TotalMilliseconds).ToString();
                        var json = JsonConvert.DeserializeObject<dynamic>(strResponse);
                        objResponse.Data = json;
                    }

                    #region Inserting In APILog 
                    objAPILogDetailListDTO = new APILogDetailListDTO();
                    objAPILogDetailListDTO.RequestXML = strRequest;
                    objAPILogDetailListDTO.ResponseXML = JsonConvert.SerializeObject(objResponse);
                    objAPILogDetailListDTO.DateCreated = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes);
                    objAPILogDetailListDTO.MethodName = MethodBase.GetCurrentMethod().Name;
                    objAPILogDetailListDTO.APIMethodId = Convert.ToInt32(General.APIMethod.GetMasters);
                    objAPILogDetailListDTO.IsRequest = true;
                    objAPILogDetailListDTO.ApiLogTypeId = objConfigurationSettingsListDTO.APILogTypeId;
                    General.objConfigurationSettingsListDTO = objConfigurationSettingsListDTO;
                    #endregion
                    if (objAPILogDetailListDTO != null && strRespectiveConnectionString != null && strRespectiveConnectionString != string.Empty)
                    {
                        try
                        {
                            General.CreateCodeLog("Step 4.1", "Satrting of InsertAPILog calling - Response", objAPILogDetailListDTO, MethodBase.GetCurrentMethod().Name);
                            Task tskInsert = Task.Run(() =>
                            {
                                long[] intAPILog = General.InsertAPILog(objAPILogDetailListDTO, strRespectiveConnectionString, objConfigurationSettingsListDTO.EncryptionKey, objConfigurationSettingsListDTO.LogTypeId, objConfigurationSettingsListDTO.CodeLogRequired, lstSiteDetails, lstSiteSolrURlListDTO);
                            });
                            General.CreateCodeLog("Step 4.2", "Ending of InsertAPILog calling - Response", "", MethodBase.GetCurrentMethod().Name);
                        }
                        catch (Exception Ex)
                        {
                            ErrorCode = Convert.ToInt32(General.ErrorCode.Technical_Error_occured);
            
                            General.CreateErrorLog(Ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                            
                        }
                    }

                    objResponse.ResponseTime = Math.Round((RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes) - startResponseTime).TotalMilliseconds).ToString();
                }
                catch (Exception ex)
                {
                    
                   General.objConfigurationSettingsListDTO = objConfigurationSettingsListDTO;
                   General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                    
                }
                #region Nullifying Objects
                //objMasterDAL = null;
                lstErrorCodeListDTO = null;
                objErrorCodeBAL = null;
                lstMasterDataListDTO = null;
                objSiteDetailListDTO = null;
                #endregion
            }
            return objResponse;
        }
        #endregion

        #region GetMasterDataByCodeInternal
        //***************************************************************************************************
        // Layer                        :   BAL 
        // Method Name                  :   GetMasterDataByCodeInternal
        // Method Description           :   This method is used to get Master Data.
        // Author                       :   Rajesh K
        // Creation Date                :   03 Aug 2023
        // Input Parameters             :   strMasterDataCode,strRespectiveConnectionString
        // Modified Date                : 
        // Modified Reason              :
        // Return Values                :   List<MasterListDTO>
        //----------------------------------------------------------------------------------------------------
        //  Version             Author                      Date                        Remarks       
        // ---------------------------------------------------------------------------------------------------
        //  1.0                 Rajesh K                    03 Aug 2023                 Creation
        //****************************************************************************************************
        /// <summary>
        /// <c>MasterData</c> This method is used to get Master Data.
        /// <param>strMasterDataCode,strRespectiveConnectionString</param>
        /// <returns>List<MasterListDTO></returns> //It returns the List.
        /// </summary>
        public DataTable GetMasterDataByCodeInternal(string strMasterDataCode, string strMasterCode, List<SiteDetails> lstSiteDetails, List<SiteSolrURlListDTO> lstSiteSolrURlListDTO)
        {
            MySqlMasterDAL objMySqlMasterDAL = null;
            MasterRequestListDTO objMasterListDTO = null;
            //List<MasterDataListDTO> lstMasterListDTO = null;
            DataTable dtGetMasterCode = null;
            MasterDAL objMasterDAL = null;
            try
            {
                objMasterListDTO = new MasterRequestListDTO();
                objMasterListDTO.MasterDataCode = strMasterDataCode;
                objMasterListDTO.CountryId = 0;
                objMasterListDTO.CurrencyId = 0;
                objMasterListDTO.LanguageId = 0;
                objMasterListDTO.SearchWord = string.Empty;
                objMasterListDTO.Code = strMasterCode;
                objMasterListDTO.IsPublished = true;    //Added by Rajesh K on 16 Aug 2023
                General.CreateCodeLog("Step 1.0", "Before Calling the GetMasterDataByCodeInternal", "", MethodBase.GetCurrentMethod().Name);

                if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MsSql))
                {
                    objMasterDAL = new MasterDAL(objConfigurationSettingsListDTO, strConnectionString);
                    //lstMasterListDTO = new List<MasterDataListDTO>(objMasterDAL.GetMasterByCode(objMasterListDTO));
                    dtGetMasterCode = objMasterDAL.GetMasterDataByCodeInternal(objMasterListDTO);
                }
                else if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MySql))
                {
                    objMySqlMasterDAL = new MySqlMasterDAL(objConfigurationSettingsListDTO, strConnectionString);
                    //lstMasterListDTO = new List<MasterDataListDTO>(objMasterDAL.GetMasterByCode(objMasterListDTO));
                    dtGetMasterCode = objMySqlMasterDAL.GetMasterDataByCodeInternal(objMasterListDTO);
                }
                General.CreateCodeLog("Step 1.1", "After Calling the GetMasterDataByCodeInternal", "", MethodBase.GetCurrentMethod().Name);
            }
            catch (Exception ex)
            {
                General.objConfigurationSettingsListDTO = objConfigurationSettingsListDTO;
                
                 General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                
            }
            finally
            {
                objMasterDAL = null;
                objMasterListDTO = null;
            }
            return dtGetMasterCode;
        }
        #endregion

        #region GetMasterDataByCodeInternalObject
        //***************************************************************************************************
        // Layer                        :   BAL 
        // Method Name                  :   GetMasterDataByCodeInternal
        // Method Description           :   This method is used to get Master Data.
        // Author                       :   Rajesh K
        // Creation Date                :   03 Aug 2023
        // Input Parameters             :   strMasterDataCode,strRespectiveConnectionString
        // Modified Date                : 
        // Modified Reason              :
        // Return Values                :   List<MasterListDTO>
        //----------------------------------------------------------------------------------------------------
        //  Version             Author                      Date                        Remarks       
        // ---------------------------------------------------------------------------------------------------
        //  1.0                 Rajesh K                    03 Aug 2023                 Creation
        //****************************************************************************************************
        /// <summary>
        /// <c>MasterData</c> This method is used to get Master Data.
        /// <param>strMasterDataCode,strRespectiveConnectionString</param>
        /// <returns>List<MasterListDTO></returns> //It returns the List.
        /// </summary>
        public object GetMasterDataByCodeInternalObject(string strMasterDataCode, string strMasterCode, List<SiteDetails> lstSiteDetails, List<SiteSolrURlListDTO> lstSiteSolrURlListDTO)
        {
            MySqlMasterDAL objMySqlMasterDAL = null;
            MasterRequestListDTO objMasterListDTO = null;
            //List<MasterDataListDTO> lstMasterListDTO = null;
            DataTable dtGetMasterCode = null;
            List<DataRow> lstRow = null;
            List<MasterDataListDTO> lstMasterListDTO = null;
            MasterDAL objMasterDAL = null;
            object objectId = null;
            try
            {
                objMasterListDTO = new MasterRequestListDTO();
                objMasterListDTO.MasterDataCode = strMasterDataCode;
                objMasterListDTO.CountryId = 0;
                objMasterListDTO.CurrencyId = 0;
                objMasterListDTO.LanguageId = 0;
                objMasterListDTO.SearchWord = string.Empty;
                objMasterListDTO.Code = strMasterCode;
                objMasterListDTO.IsPublished = true;    //Added by Rajesh K on 16 Aug 2023
                General.CreateCodeLog("Step 1.0", "Before Calling the GetMasterDataByCodeInternal", "", MethodBase.GetCurrentMethod().Name);
                if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MsSql))
                {
                    objMasterDAL = new MasterDAL(objConfigurationSettingsListDTO, strConnectionString);
                    //lstMasterListDTO = new List<MasterDataListDTO>(objMasterDAL.GetMasterByCode(objMasterListDTO));
                    dtGetMasterCode = objMasterDAL.GetMasterDataByCodeInternal(objMasterListDTO);

                }
                else if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MySql))
                {
                    objMySqlMasterDAL = new MySqlMasterDAL(objConfigurationSettingsListDTO, strConnectionString);
                    //lstMasterListDTO = new List<MasterDataListDTO>(objMasterDAL.GetMasterByCode(objMasterListDTO));
                    dtGetMasterCode = objMySqlMasterDAL.GetMasterDataByCodeInternal(objMasterListDTO);
                }
                General.CreateCodeLog("Step 1.1", "After Calling the GetMasterDataByCodeInternal", "", MethodBase.GetCurrentMethod().Name);

                if (dtGetMasterCode != null && dtGetMasterCode.Rows.Count > 0)
                { 
                //    lstRow = new List<DataRow>(dtGetMasterCode.Select());
                //    lstMasterListDTO = CommonDAL.ConvertToList<MasterDataListDTO>(lstRow);

                //    if (lstMasterListDTO != null && lstMasterListDTO.Count > 0)
                //    {
                //        objectId = lstMasterListDTO[0].Id;
                //    }


                    if(dtGetMasterCode.Rows[0]["ID"]!=null)
                    {
                        objectId = dtGetMasterCode.Rows[0]["ID"];
                    }


                }
            }
            catch (Exception ex)
            {
                General.objConfigurationSettingsListDTO = objConfigurationSettingsListDTO;
                
                 General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                
            }
            finally
            {
                objMasterDAL = null;
                objMasterListDTO = null;
            }
            return objectId;
        }
        #endregion

        #region GetMasterDataByCodeInternalAllObject
        //***************************************************************************************************
        // Layer                        :   BAL 
        // Method Name                  :   GetMasterDataByCodeInternal
        // Method Description           :   This method is used to get Master Data.
        // Author                       :   Rajesh K
        // Creation Date                :   03 Aug 2023
        // Input Parameters             :   strMasterDataCode,strRespectiveConnectionString
        // Modified Date                : 
        // Modified Reason              :
        // Return Values                :   List<MasterListDTO>
        //----------------------------------------------------------------------------------------------------
        //  Version             Author                      Date                        Remarks       
        // ---------------------------------------------------------------------------------------------------
        //  1.0                 Rajesh K                    03 Aug 2023                 Creation
        //****************************************************************************************************
        /// <summary>
        /// <c>MasterData</c> This method is used to get Master Data.
        /// <param>strMasterDataCode,strRespectiveConnectionString</param>
        /// <returns>List<MasterListDTO></returns> //It returns the List.
        /// </summary>
        public object GetMasterDataByCodeInternalAllObject(string strMasterDataCode, string strMasterCode, List<SiteDetails> lstSiteDetails, List<SiteSolrURlListDTO> lstSiteSolrURlListDTO)
        {
            MySqlMasterDAL objMySqlMasterDAL = null;
            MasterRequestListDTO objMasterListDTO = null;
            //List<MasterDataListDTO> lstMasterListDTO = null;
            DataTable dtGetMasterCode = null;
            List<DataRow> lstRow = null;
            List<MasterDataListDTO> lstMasterListDTO = null;
            MasterDAL objMasterDAL = null;
            object objectId = null;
            try
            {
                objMasterListDTO = new MasterRequestListDTO();
                objMasterListDTO.MasterDataCode = strMasterDataCode;
                objMasterListDTO.CountryId = 0;
                objMasterListDTO.CurrencyId = 0;
                objMasterListDTO.LanguageId = 0;
                objMasterListDTO.SearchWord = string.Empty;
                objMasterListDTO.Code = strMasterCode;
                General.CreateCodeLog("Step 1.0", "Before Calling the GetMasterDataByCodeInternal", "", MethodBase.GetCurrentMethod().Name);
                if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MsSql))
                {
                    objMasterDAL = new MasterDAL(objConfigurationSettingsListDTO, strConnectionString);
                    //lstMasterListDTO = new List<MasterDataListDTO>(objMasterDAL.GetMasterByCode(objMasterListDTO));
                    dtGetMasterCode = objMasterDAL.GetMasterDataByCodeInternal(objMasterListDTO);

                }
                else if (objConfigurationSettingsListDTO.DataBaseTypeId == Convert.ToInt32(General.DataBaseTypeId.MySql))
                {
                    objMySqlMasterDAL = new MySqlMasterDAL(objConfigurationSettingsListDTO, strConnectionString);
                    //lstMasterListDTO = new List<MasterDataListDTO>(objMasterDAL.GetMasterByCode(objMasterListDTO));
                    dtGetMasterCode = objMySqlMasterDAL.GetMasterDataByCodeInternal(objMasterListDTO);
                }
                General.CreateCodeLog("Step 1.1", "After Calling the GetMasterDataByCodeInternal", "", MethodBase.GetCurrentMethod().Name);

                if (dtGetMasterCode != null && dtGetMasterCode.Rows.Count > 0)
                {
                    //    lstRow = new List<DataRow>(dtGetMasterCode.Select());
                    //    lstMasterListDTO = CommonDAL.ConvertToList<MasterDataListDTO>(lstRow);

                    //    if (lstMasterListDTO != null && lstMasterListDTO.Count > 0)
                    //    {
                    //        objectId = lstMasterListDTO[0].Id;
                    //    }


                    if (dtGetMasterCode.Rows[0]["ID"] != null)
                    {
                        objectId = dtGetMasterCode.Rows[0]["ID"];
                    }


                }
            }
            catch (Exception ex)
            {
                General.objConfigurationSettingsListDTO = objConfigurationSettingsListDTO;

                General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);

            }
            finally
            {
                objMasterDAL = null;
                objMasterListDTO = null;
            }
            return objectId;
        }
        #endregion













        #region GetUserDetailByPrivateKey
        //-------------------------------------------------------------------------------------------------------------------
        //Method Name                   :   GetUserDetailByPrivateKey
        //Method Description			:	This method is used to get the User details by Id.
        //Author						:	Upendra G
        //Creation Date         		:   01 Aug 2023
        //--------------------------------------------------------------------------------------------------------------------
        // Version         Author                            Date                        Remarks       
        // ------------------------------------------------------------------------------------------------------------
        // 1.0.0    	 Upendra G                       01 Aug 2023                 Creation
        //*************************************************************************************************************
        /// <summary>
        /// <c>GetUserByIdDB : </c> This method is used to get the User details by Id.
        /// </summary>
        /// <param name="objUserListDTO"></param>
        /// <returns></returns>
        /// </summary>       

        public List<MasterListDTO> GetUserDetailByPrivateKeyDB(string PrivateKey)
        {
            List<MasterListDTO> lstMasterListDTO = null;

            using (SqlConnection connection = new SqlConnection(strConnectionString))
            {
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
                List<DataRow> lstRow = null;
                using (SqlCommand sqlCmd = new SqlCommand())
                {
                    sqlCmd.Connection = connection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandTimeout = CommandTimeOut;
                    sqlCmd.CommandText = "usp_User_GetUserDetailByPrivateKey";
                    sqlCmd.Parameters.Add("@PrivateKey", SqlDbType.NVarChar).Value = PrivateKey;
                    sqlCmd.Parameters.Add("IsAuthenticateCheck", SqlDbType.Bit).Value = IsCheckAuthenticate;
                    SqlDataAdapter daUser = new SqlDataAdapter(sqlCmd);
                    DataTable dtUser = new DataTable();
                    daUser.Fill(dtUser);

                    if (dtUser != null && dtUser.Rows.Count > 0)
                    {
                        lstMasterListDTO = new List<MasterListDTO>();
                        lstRow = new List<DataRow>(dtUser.Select());
                        lstMasterListDTO = CommonDAL.ConvertToList<MasterListDTO>(lstRow);
                    }
                }

                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
            }

            return lstMasterListDTO;
        }
        #endregion

        #region GetMasterDataByCodeInternal
        //-------------------------------------------------------------------------------------------------------------------
        //Method Name                   :   GetMasterDataByCodeInternal
        //Method Description			:	This method is used to get the Master data details by MasterDataCode for Validating Masters.
        //Author						:	Rajesh K
        //Creation Date         		:   16 Aug 2023
        //--------------------------------------------------------------------------------------------------------------------
        // Version         Author                            Date                        Remarks       
        // ------------------------------------------------------------------------------------------------------------
        // 1.0.0    	   Rajesh K                      16 Aug 2023                 Creation
        //*************************************************************************************************************
        /// <summary>
        /// <c>GetMasterDataByCodeInternal : </c> This method is used to get the Master data details by MasterDataCode for Validating Masters.
        /// </summary>
        /// <param name="objMasterListDTO"></param>
        /// <returns></returns>
        /// </summary>    
        public DataTable GetMasterDataByCodeInternal(MasterRequestListDTO objMasterListDTO)
        {
            using (SqlConnection connection = new SqlConnection(strConnectionString))
            {
                connection.Open();
                using (SqlCommand objSqlCmd = new SqlCommand())
                {
                    objSqlCmd.Connection = connection;
                    objSqlCmd.CommandType = CommandType.StoredProcedure;
                    objSqlCmd.CommandText = @"dbo.usp_MasterData_GetMasterDataByCodeInternal";
                    objSqlCmd.Parameters.Add("@MasterDataCode", SqlDbType.NVarChar).Value = objMasterListDTO.MasterDataCode;
                    objSqlCmd.Parameters.Add("@CountryId", SqlDbType.Int).Value = objMasterListDTO.CountryId;
                    objSqlCmd.Parameters.Add("@CurrencyId", SqlDbType.Int).Value = objMasterListDTO.CurrencyId;
                    objSqlCmd.Parameters.Add("@LanguageId", SqlDbType.Int).Value = objMasterListDTO.LanguageId;
                    objSqlCmd.Parameters.Add("@SearchWord", SqlDbType.NVarChar).Value = objMasterListDTO.SearchWord;
                    objSqlCmd.Parameters.Add("@Code", SqlDbType.NVarChar).Value = objMasterListDTO.Code;
                    objSqlCmd.Parameters.Add("@IsPublished", SqlDbType.Bit).Value = objMasterListDTO.IsPublished;
                    SqlDataAdapter da = new SqlDataAdapter(objSqlCmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }
        #endregion

        #region GetMasterDataByCode
        //-------------------------------------------------------------------------------------------------------------------
        //Method Name                   :   GetMasterDataByCode
        //Method Description			:	This method is used to get the Master data details by MasterDataCode for Validating Masters.
        //Author						:	Rajesh K
        //Creation Date         		:   16 Aug 2023
        //--------------------------------------------------------------------------------------------------------------------
        // Version         Author                            Date                        Remarks       
        // ------------------------------------------------------------------------------------------------------------
        // 1.0.0    	   Rajesh K                      16 Aug 2023                 Creation
        //*************************************************************************************************************
        /// <summary>
        /// <c>GetMasterDataByCode : </c> This method is used to get the Master data details by MasterDataCode for Validating Masters.
        /// </summary>
        /// <param name="objMasterListDTO"></param>
        /// <returns></returns>
        /// </summary>    
        public DataTable GetMasterDataByCode(MasterRequestDTO objMasterListDTO)
        {
            using (SqlConnection connection = new SqlConnection(strConnectionString))
            {
                connection.Open();
                using (SqlCommand objSqlCmd = new SqlCommand())
                {
                    objSqlCmd.Connection = connection;
                    objSqlCmd.CommandType = CommandType.StoredProcedure;
                    objSqlCmd.CommandText = @"dbo.usp_MasterData_GetMasterDataByCode";
                    objSqlCmd.Parameters.Add("@MasterDataCode", SqlDbType.NVarChar).Value = objMasterListDTO.MasterDataCode;
                    objSqlCmd.Parameters.Add("@CountryId", SqlDbType.Int).Value = objMasterListDTO.CountryId;
                    objSqlCmd.Parameters.Add("@CurrencyId", SqlDbType.Int).Value = objMasterListDTO.CurrencyId;
                    objSqlCmd.Parameters.Add("@LanguageId", SqlDbType.Int).Value = objMasterListDTO.LanguageId;
                    objSqlCmd.Parameters.Add("@SearchWord", SqlDbType.NVarChar).Value = objMasterListDTO.SearchWord;
                    objSqlCmd.Parameters.Add("@Code", SqlDbType.NVarChar).Value = objMasterListDTO.Code;
                    objSqlCmd.Parameters.Add("@IsPublished", SqlDbType.Bit).Value = objMasterListDTO.IsPublished;
                    SqlDataAdapter da = new SqlDataAdapter(objSqlCmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }
        #endregion

        #region GetSiteDetailsbySiteid
        //-------------------------------------------------------------------------------------------------------------------
        //Method Name                   :   GetSiteDetailsbySiteid
        //Method Description			:	This method is used to get the Site details.
        //Author						:	Anandan S
        //Creation Date         		:   14 Oct 2024
        //--------------------------------------------------------------------------------------------------------------------
        // Version         Author                            Date                        Remarks       
        // ------------------------------------------------------------------------------------------------------------
        // 1.0.0    	  Akhila R                       24 June 2025                 Creation
        //*************************************************************************************************************
        /// <summary>
        /// <c>GetUserByIdDB : </c> This method is used to get the Site details.
        /// </summary>
        /// <returns></returns>
        /// </summary>       
        /// 
        public List<SiteDetails> GetSiteDetailsbySiteid(string strSitecode, int intCountryId, int intCurrencyId, int intLanguageId)
        {
            List<SiteDetails> lstSiteDetails = null;
            List<DataRow> lstRow = null;
            using (SqlConnection connection = new SqlConnection(strConnectionString))
            {

                lstSiteDetails = new List<SiteDetails>();
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
                using (SqlCommand sqlCmd = new SqlCommand())
                {
                    sqlCmd.Connection = connection;
                    sqlCmd.CommandType = System.Data.CommandType.StoredProcedure;
                    sqlCmd.CommandTimeout = CommandTimeOut;
                    sqlCmd.CommandText = @"dbo.usp_Site_GetSiteDetailBySiteCode";
                    sqlCmd.Parameters.Add("Code", SqlDbType.NVarChar).Value = strSitecode;
                    sqlCmd.Parameters.Add("CountryId", SqlDbType.Int).Value = intCountryId;
                    sqlCmd.Parameters.Add("CurrencyId", SqlDbType.Int).Value = intCurrencyId;
                    sqlCmd.Parameters.Add("LanguageId", SqlDbType.Int).Value = intLanguageId;
                    SqlDataAdapter daLogin = new SqlDataAdapter(sqlCmd);
                    DataTable dtLogin = new DataTable();
                    daLogin.Fill(dtLogin);
                    if (dtLogin != null && dtLogin.Rows.Count > 0)
                    {
                        lstRow = new List<DataRow>(dtLogin.Select());
                        lstSiteDetails = General.ConvertToList<SiteDetails>(lstRow);
                    }
                }

                if (connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }

                if (lstSiteDetails != null && lstSiteDetails.Count > 0)
                {
                    return lstSiteDetails;
                }
                else
                {
                    return null;
                }
            }
        }
        #endregion


       
        #region GetMasterAPI
        //-------------------------------------------------------------------------------------------------------------------
        // Method Name              :   GetMasterDataByCode
        // Method Description        :   This method is used to get the Master API details by MasterDataCode.
        // Author                    :   Nagaraju K
        // Creation Date             :   11 Oct 2025
        //--------------------------------------------------------------------------------------------------------------------
        // Version         Author          Date              Remarks       
        // ------------------------------------------------------------------------------------------------------------
        // 1.0.0           Nagaraju K      11 Oct 2025       Creation
        //*************************************************************************************************************
        /// <summary>
        /// <c>GetMasterDataByCode :</c>
        /// This method retrieves Master API details based on the provided MasterDataCode. 
        /// It dynamically executes the stored procedure specified in the request DTO, 
        /// populates parameters such as CountryId, CurrencyId, LanguageId, SearchWord, and others,
        /// and returns the resulting data as a DataTable.
        /// </summary>
        /// <param name="objMasterListDTO">Object containing MasterDataCode, StoredProcedureName, and filter parameters.</param>
        /// <returns>DataTable containing Master API details.</returns>
        //*************************************************************************************************************

        public DataTable GetMasterDataByCode(MasterRequestListDTO objMasterListDTO)
        {
            using (SqlConnection connection = new SqlConnection(strConnectionString))
            {
                connection.Open();
                using (SqlCommand objSqlCmd = new SqlCommand())
                {
                    objSqlCmd.Connection = connection;
                    objSqlCmd.CommandType = CommandType.StoredProcedure;
                    objSqlCmd.CommandText = objMasterListDTO.StoredProcedureName;
                    objSqlCmd.Parameters.Add("@MasterDataCode", SqlDbType.NVarChar).Value = objMasterListDTO.MasterDataCode;
                    objSqlCmd.Parameters.Add("@CountryId", SqlDbType.Int).Value = objMasterListDTO.CountryId;
                    objSqlCmd.Parameters.Add("@CurrencyId", SqlDbType.Int).Value = objMasterListDTO.CurrencyId;
                    objSqlCmd.Parameters.Add("@LanguageId", SqlDbType.Int).Value = objMasterListDTO.LanguageId;
                    objSqlCmd.Parameters.Add("@SearchWord", SqlDbType.NVarChar).Value = objMasterListDTO.SearchWord;
                    objSqlCmd.Parameters.Add("@Code", SqlDbType.NVarChar).Value = objMasterListDTO.Code;
                    objSqlCmd.Parameters.Add("@UserId", SqlDbType.NVarChar).Value = objMasterListDTO.UserId;
                    //objSqlCmd.Parameters.Add("@PageNumber", SqlDbType.NVarChar).Value = objMasterListDTO.PageNumber;
                    //objSqlCmd.Parameters.Add("@PageSize", SqlDbType.NVarChar).Value = objMasterListDTO.PageSize;
                    objSqlCmd.Parameters.Add("@IsPublished", SqlDbType.Bit).Value = objMasterListDTO.IsActive;

                    if (!String.IsNullOrWhiteSpace(objMasterListDTO.StoreCode))   //Added by Rajesh K on 22 Aug 2023
                    {
                        objSqlCmd.Parameters.Add("@StoreId", SqlDbType.NVarChar).Value = objMasterListDTO.StoreCode;
                    }
                    else
                    {
                        objSqlCmd.Parameters.Add("@StoreId", SqlDbType.NVarChar).Value = System.DBNull.Value;
                    }

                    SqlDataAdapter da = new SqlDataAdapter(objSqlCmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }
        #endregion

        #region  GetSpNameByCode
        //-------------------------------------------------------------------------------------------------------------------
        // Method Name              :   GetSpNameByCode
        // Method Description        :   This method is used to retrieve the MasterCodeSpName for a given MasterDataCode.
        // Author                    :   Nagaraju K
        // Creation Date             :   11 Oct 2025
        //--------------------------------------------------------------------------------------------------------------------
        // Version         Author          Date              Remarks       
        // ------------------------------------------------------------------------------------------------------------
        // 1.0.0           Nagaraju K      11 Oct 2025       Creation
        //*************************************************************************************************************
        /// <summary>
        /// <c>GetSpNameByCode :</c>
        /// This method executes the stored procedure <b>usp_MasterData_GetSpNameByCode</b> 
        /// to fetch the <b>MasterCodeSpName</b> for the provided MasterDataCode.  
        /// It uses <see cref="ExecuteScalar"/> to retrieve a single value directly from SQL Server.
        /// If a valid record is found, the method returns the MasterCodeSpName as a string; 
        /// otherwise, it returns null.
        /// </summary>
        /// <param name="objMasterListDTO">
        /// The object containing filter parameters such as MasterDataCode, CountryId, CurrencyId, and LanguageId.
        /// </param>
        /// <returns>
        /// A string representing the <b>MasterCodeSpName</b> if found; otherwise, null.
        /// </returns>
        //*************************************************************************************************************

        public string GetSpNameByCode(MasterRequestListDTO objMasterListDTO)
        {
            using (SqlConnection connection = new SqlConnection(strConnectionString))
            {
                connection.Open();
                using (SqlCommand objSqlCmd = new SqlCommand("dbo.usp_MasterData_GetSpNameByCode", connection))
                {
                    objSqlCmd.CommandType = CommandType.StoredProcedure;

                    objSqlCmd.Parameters.Add("@MasterDataCode", SqlDbType.NVarChar, 64).Value = objMasterListDTO.MasterDataCode ?? (object)DBNull.Value;
                    objSqlCmd.Parameters.Add("@CountryId", SqlDbType.Int).Value = objMasterListDTO.CountryId;
                    objSqlCmd.Parameters.Add("@CurrencyId", SqlDbType.Int).Value = objMasterListDTO.CurrencyId;
                    objSqlCmd.Parameters.Add("@LanguageId", SqlDbType.Int).Value = objMasterListDTO.LanguageId;
                    
                    // Execute and read only the first column (MasterCodeSpName)
                    object result = objSqlCmd.ExecuteScalar();

                    return result != null && result != DBNull.Value
                        ? Convert.ToString(result)
                        : null;
                }
            }
        }
        #endregion








#region Communication Alert
if (ErrorCode == 0) //Added By Muni B (BRD: RDLC - REV0036/LOGIN/RDLC/787)
{
    var item = lstRequestObject.Where(x => x.Key.ToLower() == "Id".ToLower()).Select(obj => obj).FirstOrDefault();
    string value = item.Value, strRFQPortalURL = string.Empty;
    bool IsEmailEnableInEditMode = lstAPITemplateColumnListDTO.Any(x => x.IsEmail && x.IsEmailEnabled && x.IsEmailEnableInEditMode);

    #region RFQPortalURL getting from Site Configuraton
    if (dtResult != null && dtResult.Rows.Count > 0 && ErrorCode == 0 && intModuleId == Convert.ToInt32(SAASPOSGeneral.Module.Vendor))  // Added By AKhila R on 19 Dec 2025
    {
        if (ErrorCode == 0)
        {
            objCommonBAL = new CommonBAL(_objConfigurationSettingsListDTO, RespectiveConnectionString);
            object objPortalURL = objCommonBAL.GetSiteConfiguration(Convert.ToInt32(SAASPOSGeneral.SiteConfiguration.RFQPortalURL), intCountryId, intCurrencyId, intLanguageId);

            if(objPortalURL!= null)
            {
                strRFQPortalURL = string.IsNullOrWhiteSpace(objPortalURL?.ToString()) ? string.Empty: objPortalURL.ToString().Split('?')[0];
            }
        }
    }
    #endregion

    if ((string.IsNullOrWhiteSpace(value) || IsEmailEnableInEditMode) && ErrorCode == 0)
    {
        #region Email Alert Message

        if (ErrorCode == 0)
        {
            bool IsEmailTrue = lstAPITemplateColumnListDTO.Any(x => x.IsEmail && x.IsEmailEnabled && !string.IsNullOrEmpty(x.AlertTypeCode));

            // Added for ContactUs Mobile country code
            if (IsEmailTrue && dtResult != null)
            {
                if (AllMasterDataObject != null)
                {
                    foreach (var masterRow in AllMasterDataObject)
                    {
                        foreach (var kvp in masterRow)
                        {
                            // Add or overwrite
                            RequestObject[kvp.Key] = kvp.Value;
                        }
                    }
                }

                Task tskAlert = Task.Run(() =>
                {
                    General.CreateCodeLog("Step 4.7", "before InsertAPILog method", "", strMethodName, lstSiteDetails);

                    objEmailAlert = new EmailAlert(_objConfigurationSettingsListDTO, RespectiveConnectionString);
                    objEmailAlert.sendEmailAlert(RequestObject, RequestChildObjectData, lstAPITemplateColumnListDTO, RespectiveConnectionString, _objConfigurationSettingsListDTO.EncryptionKey, _objConfigurationSettingsListDTO.LogTypeId, _objConfigurationSettingsListDTO.CodeLogRequired, lstSiteDetails, dtResult, strRFQPortalURL);

                    General.CreateCodeLog("Step 4.8", "After InsertAPILog method", "", strMethodName, lstSiteDetails);
                });
            }
        }
        #endregion

        #region SMS Alert Message

        if (ErrorCode == 0)
        {
            bool IsSMSTrue = lstAPITemplateColumnListDTO.Any(x => x.IsSMS && x.IsSMSEnabled && !string.IsNullOrEmpty(x.AlertTypeCode));
            if (IsSMSTrue)
            {
                Task tskAlert = Task.Run(() =>
                {
                    General.CreateCodeLog("Step 4.7", "before InsertAPILog method", "", strMethodName, lstSiteDetails);

                    objSMSAlert = new SMSAlert(_objConfigurationSettingsListDTO, RespectiveConnectionString);
                    objSMSAlert.sendSMSAlert(RequestObject, lstAPITemplateColumnListDTO, RespectiveConnectionString, _objConfigurationSettingsListDTO.EncryptionKey, _objConfigurationSettingsListDTO.LogTypeId, _objConfigurationSettingsListDTO.CodeLogRequired, lstSiteDetails, dtResult);

                    General.CreateCodeLog("Step 4.8", "After InsertAPILog method", "", strMethodName, lstSiteDetails);
                });
            }
        }
        #endregion

        #region WhatsApp Alert Message

        if (ErrorCode == 0)
        {
            bool IsWhatsAppTrue = lstAPITemplateColumnListDTO.Any(x => x.IsWhatsApp && x.IsWhatsAppEnabled && !string.IsNullOrEmpty(x.AlertTypeCode));

            if (IsWhatsAppTrue)
            {
                Task tskAlert = Task.Run(() =>
                {
                    General.CreateCodeLog("Step 4.7", "before InsertAPILog method", "", strMethodName, lstSiteDetails);

                    objWhatsAppAlertNotification = new WhatsAppAlertNotification(_objConfigurationSettingsListDTO, RespectiveConnectionString);
                    objWhatsAppAlertNotification.sendWhatsAppAlert(RequestObject, lstAPITemplateColumnListDTO, RespectiveConnectionString, _objConfigurationSettingsListDTO.EncryptionKey, _objConfigurationSettingsListDTO.LogTypeId, _objConfigurationSettingsListDTO.CodeLogRequired, lstSiteDetails, dtResult);

                    General.CreateCodeLog("Step 4.8", "After InsertAPILog method", "", strMethodName, lstSiteDetails);
                });
            }
        }
        #endregion                                               

        #region Admin Email Alert Message

        if (ErrorCode == 0)
        {
            bool IsAdminEmailTrue = lstAPITemplateColumnListDTO.Any(x => x.IsAdminEmail && x.IsEmailEnabled && !string.IsNullOrEmpty(x.AdminAlertTypeCode));

            if (IsAdminEmailTrue && dtResult != null)
            {
                Task tskAlert = Task.Run(() =>
                {
                    General.CreateCodeLog("Step 4.7", "before InsertAPILog method", "", strMethodName, lstSiteDetails);

                    objEmailAlert = new EmailAlert(_objConfigurationSettingsListDTO, RespectiveConnectionString);
                    objEmailAlert.sendEmailAlert(RequestObject, RequestChildObjectData, lstAPITemplateColumnListDTO, RespectiveConnectionString, _objConfigurationSettingsListDTO.EncryptionKey, _objConfigurationSettingsListDTO.LogTypeId, _objConfigurationSettingsListDTO.CodeLogRequired, lstSiteDetails, dtResult, strRFQPortalURL, IsAdminEmailTrue);

                    General.CreateCodeLog("Step 4.8", "After InsertAPILog method", "", strMethodName, lstSiteDetails);
                });
            }
        }
        #endregion
    }

}
#endregion









        public void sendEmailAlert(IDictionary<dynamic, dynamic> dictObject, IDictionary<dynamic, dynamic> dictChildObject, List<APITemplateColumnListDTO> lstAPITemplateColumnListDTO, string strConnectionString, string Key, int LogTypeId, int CodeLogRequired, List<SiteDetails> lstSiteDetails, DataTable dtResult,string strRFQPortalURL="",bool IsAdminEmail = false)
        {
            int intEmailTemplateId = 0, ErrorCode = 0;
            APITemplateDAL objAPITemplateDAL = null;
            dynamic value = null;
            StoreEmailTemplateBAL objStoreEmailTemplateBAL = null;
            string columnName = string.Empty, alertTypeCode = string.Empty, strAESEncryptkey = string.Empty, strModuleName = string.Empty,
                strAESEncryptiv = string.Empty, strEmailContent = string.Empty, strUserEmail = string.Empty, strTableName = string.Empty,
                strUsernameColumn = string.Empty, strFirstName = string.Empty, strDecryptUserEmail = string.Empty, strDecryptPassword = string.Empty,
                strEmailSubjet = string.Empty, strPassword = string.Empty;            
            List<StoreEmailTemplateListDTO> lstStoreEmailTemplateListDTO = null;
            StoreEmailLogListDTO objStoreEmailLogListDTO = null;            
            ErrorCodeBAL objErrorCodeBAL = null;   //Added by Manisha on 24 July 2024
            List<ErrorCodeListDTO> lstErrorCodeListDTO = null;           
            SearchDataDAL objSearchDataDAL = null;
            DataTable dtGetSearchData = null;
            string strMethodName = MethodBase.GetCurrentMethod().Name;
            List<APITemplateColumnListDTO> lstPasswordAPITemplateColumnListDTO = null;
            CommonBAL objCommonBAL = null;
            bool bolValue = false, IsCredentialsSendMail = false;
            try
            {
                #region Encryptionkey 
                if (ErrorCode == 0)
                {
                    if (!string.IsNullOrEmpty(_objConfigurationSettingsListDTO.AESEncryptkey))
                    {
                        strAESEncryptkey = clsCryptography.Decrypt(_objConfigurationSettingsListDTO.AESEncryptkey, _objConfigurationSettingsListDTO.EncryptionKey);
                    }
                    if (!string.IsNullOrEmpty(_objConfigurationSettingsListDTO.AESEncryptiv))
                    {
                        strAESEncryptiv = clsCryptography.Decrypt(_objConfigurationSettingsListDTO.AESEncryptiv, _objConfigurationSettingsListDTO.EncryptionKey);
                    }
                }
                #endregion

                if (lstAPITemplateColumnListDTO != null && lstAPITemplateColumnListDTO.Count > 0)
                {

                    if (dictObject == null || lstAPITemplateColumnListDTO == null)
                    {
                        Console.WriteLine("One or both inputs are null.");
                        return;
                    }
                    strModuleName = lstAPITemplateColumnListDTO[0].APITemplateName;

                    var passwordColumn = lstAPITemplateColumnListDTO
                                    .FirstOrDefault(c => c.APITemplateColumnName.Contains("Password") && dictObject.ContainsKey(c.APITemplateColumnName));

                    if (passwordColumn != null)
                    {
                        string columnName1 = passwordColumn.APITemplateColumnName;
                        strPassword = dictObject[columnName1];
                        strDecryptPassword = clsCryptography.Decrypt(strPassword);
                    }

                    var passwordAutoGenColumn = lstAPITemplateColumnListDTO
                        .FirstOrDefault(obj => obj.IsPassword == true && obj.IsPasswordAutoGenerate == true);

                    if (passwordAutoGenColumn != null)
                    {
                        strUsernameColumn = passwordAutoGenColumn.UsernameColumn;
                    }

                    foreach (APITemplateColumnListDTO column in lstAPITemplateColumnListDTO)
                    {
                        columnName = column.APITemplateColumnName;

                        //if (column.APITemplateColumnName.Contains("Password"))
                        //{
                        //    strPassword = dictObject[columnName];
                        //    strDecryptPassword = clsCryptography.Decrypt(strPassword);
                        //}
                        //lstPasswordAPITemplateColumnListDTO = lstAPITemplateColumnListDTO.Where(obj => obj.IsPassword == true && obj.IsPasswordAutoGenerate == true).ToList();
                        //foreach (var objPasswordAPIColumnListDTO in lstPasswordAPITemplateColumnListDTO)
                        //{
                        //    strUsernameColumn = objPasswordAPIColumnListDTO.UsernameColumn;
                        //}

                        if (column.IsEmailEnabled)
                        {
                            if (IsAdminEmail)
                            {
                                alertTypeCode = column.AdminAlertTypeCode;
                            }
                            else
                            {
                                alertTypeCode = column.AlertTypeCode;
                            }
                               
                            strTableName = column.TableName;

                            #region Send Email Via Text
                            if (!string.IsNullOrEmpty(columnName) && dictObject.ContainsKey(columnName) && !column.IsMaster)
                            {
                                value = dictObject[columnName];

                                General.CreateCodeLog("Step 4.4", "Before Calling GetStoreEmailTemplateByCode", "", MethodBase.GetCurrentMethod().Name);
                                objStoreEmailTemplateBAL = new StoreEmailTemplateBAL(objConfigurationSettingsListDTO);
                                lstStoreEmailTemplateListDTO = objStoreEmailTemplateBAL.GetStoreEmailTemplateByCode(alertTypeCode, strConnectionString);
                                General.CreateCodeLog("Step 4.4", "After Calling GetStoreEmailTemplateByCode", "", MethodBase.GetCurrentMethod().Name);

                                if (lstStoreEmailTemplateListDTO != null && lstStoreEmailTemplateListDTO.Count > 0)
                                {

                                    // ===========================
                                    // This section is for Employee Module
                                    // ===========================
                                    // It retrieves the UsernameColumn (e.g., email) for the given Employee record.
                                    // It builds a dynamic query to get the value from the specified column (usually email or username).
                                    // Then, it executes the query and fetches the user email (and optionally first name if multiple columns are returned).

                                    if (!string.IsNullOrEmpty(strUsernameColumn))
                                    {
                                        string strCode = (string)dtResult.Rows[0]["Id"];
                                        string strQuery = "SELECT " + strUsernameColumn + " FROM " + strTableName + " WHERE ID ='" + strCode + "'";

                                        objSearchDataDAL = new SearchDataDAL(_objConfigurationSettingsListDTO, RespectiveConnectionString);
                                        General.CreateCodeLog("Step 4.4", "Before Calling GetDataForEmailDB", "", MethodBase.GetCurrentMethod().Name);
                                        dtGetSearchData = objSearchDataDAL.GetDataForEmailDB(strQuery);
                                        if (dtGetSearchData?.Rows.Count > 0)
                                        {
                                            strUserEmail = dtGetSearchData.Rows[0][0]?.ToString();
                                            General.CreateCodeLog("Step 4.5", "After Calling GetDataForEmailDB", "", MethodBase.GetCurrentMethod().Name);

                                            if (strUsernameColumn.Contains(','))
                                            {
                                                strFirstName = (string)dtGetSearchData.Rows[0][1];
                                            }
                                            strUserEmail = dtGetSearchData.Rows[0][0]?.ToString();
                                            strDecryptUserEmail = General.RevalDecrypt(strUserEmail, strAESEncryptkey, strAESEncryptiv);
                                        }
                                    }
                                    if (column.IsAesEncrypt)
                                    {
                                        strDecryptUserEmail = General.RevalDecrypt(value, strAESEncryptkey, strAESEncryptiv);
                                    }

                                    strEmailContent = lstStoreEmailTemplateListDTO[0].EmailTemplateContent;

                                    // Get all matches (including duplicates, e.g., [#VenderName#] repeated)
                                    var matches = Regex.Matches(strEmailContent, @"\[#(.*?)#\]");

                                    // Convert to a list to avoid issues with modifying the string during iteration
                                    var placeholderList = matches.Cast<Match>()
                                                                 .Select(m => new { Full = m.Value, Key = m.Groups[1].Value })
                                                                 .ToList();

                                    // Loop through each placeholder one by one
                                    foreach (var item in placeholderList)
                                    {
                                        string placeholder = item.Full;  // [#VenderName#]
                                        string key = item.Key;           // VenderName

                                        foreach (var kvp in dictObject)
                                        {
                                            string dictKey = Convert.ToString(kvp.Key);

                                            // If key matches (case-insensitive), replace all instances
                                            if (string.Equals(dictKey, key))
                                            {
                                                string OriginalValue = Convert.ToString(kvp.Value);

                                                // Find column definition
                                                var columnInfo = lstAPITemplateColumnListDTO
                                                            .FirstOrDefault(c => string.Equals(c.APITemplateColumnName, key, StringComparison.OrdinalIgnoreCase));

                                                if (columnInfo != null && columnInfo.IsAesEncrypt)
                                                {
                                                    OriginalValue = General.RevalDecrypt(OriginalValue, strAESEncryptkey, strAESEncryptiv);
                                                }
                                                strEmailContent = strEmailContent.Replace(placeholder, OriginalValue);
                                                // break; // done for this match
                                            }
                                            else
                                            {
                                                strEmailContent = strEmailContent.Replace("[#USERNAME#]", strFirstName);
                                                strEmailContent = strEmailContent.Replace("[#EmployeeName#]", strFirstName);
                                                strEmailContent = strEmailContent.Replace("[#ERP URL#]", lstSiteDetails[0].SiteURL);
                                                strEmailContent = strEmailContent.Replace("[#SiteURL#]", lstSiteDetails[0].SiteURL);
                                                strEmailContent = strEmailContent.Replace("[#Your User ID#]", strDecryptUserEmail);
                                                strEmailContent = strEmailContent.Replace("[#Your Password#]", strDecryptPassword);
                                                strEmailContent = strEmailContent.Replace("[#Logo#]", lstSiteDetails[0].EmailLogo);
                                                strEmailContent = strEmailContent.Replace("[#CompanyName#]", lstSiteDetails[0].SiteName);
                                                strEmailContent = strEmailContent.Replace("[#Company Name#]", lstSiteDetails[0].SiteName);
                                                strEmailContent = strEmailContent.Replace("[#SiteName#]", lstSiteDetails[0].SiteName);
                                                strEmailContent = strEmailContent.Replace("[#StoreName#]", lstSiteDetails[0].SiteName);
                                                strEmailContent = strEmailContent.Replace("[#PortalURL#]", strRFQPortalURL);
                                                strEmailContent = strEmailContent.Replace("[#ModuleName#]", strModuleName);
                                            }
                                        }
                                    }

                                    strEmailSubjet = lstStoreEmailTemplateListDTO[0].EmailTemplateSubject;
                                    strEmailSubjet = strEmailSubjet.Replace("[#Company Name#]", lstSiteDetails[0].SiteName);
                                    if (IsAdminEmail)
                                    {
                                        strDecryptUserEmail = General.RevalDecrypt(lstStoreEmailTemplateListDTO[0].EmailTemplateTo, strAESEncryptkey, strAESEncryptiv);
                                    }
                                    objStoreEmailLogListDTO = new StoreEmailLogListDTO
                                    {
                                        EmailFrom = General.RevalDecrypt(lstStoreEmailTemplateListDTO[0].EmailTemplateFrom, strAESEncryptkey, strAESEncryptiv), //Added by Hanumanthu S on 16 Oct 2024
                                        EmailCC = General.RevalDecrypt(lstStoreEmailTemplateListDTO[0].EmailTemplateCC, strAESEncryptkey, strAESEncryptiv), //Added by Hanumanthu S on 16 Oct 2024
                                        EmailBCC = General.RevalDecrypt(lstStoreEmailTemplateListDTO[0].EmailTemplateBCC, strAESEncryptkey, strAESEncryptiv), //Added by Hanumanthu S on 16 Oct 2024
                                        EmailSubject = strEmailSubjet,
                                        StoreEmailTemplateId = lstStoreEmailTemplateListDTO[0].StoreEmailTemplateId,
                                        SiteId = SiteId,
                                        EmailContent = strEmailContent,
                                        EmailTo = strDecryptUserEmail,
                                        SendEmail = true,
                                        MailUserName = General.RevalDecrypt(lstStoreEmailTemplateListDTO[0].MailUserName, strAESEncryptkey, strAESEncryptiv), //Added by Hanumanthu S on 16 Oct 2024
                                        MailPassword = lstStoreEmailTemplateListDTO[0].MailPassword,
                                        PortNo = lstStoreEmailTemplateListDTO[0].PortNo,
                                        MailServerName = lstStoreEmailTemplateListDTO[0].MailServerName,
                                        EnableSSL = lstStoreEmailTemplateListDTO[0].EnableSSL,
                                        DisplayName = lstStoreEmailTemplateListDTO[0].DisplayName,   // Added By Akhila R on 23 Jan 2024
                                        AlertTypeId = lstStoreEmailTemplateListDTO[0].AlertTypeId    // Added By Srinivas M on 03 Feb 2024
                                    };

                                    try
                                    {
                                        //General.CreateCodeLog("Step 4.2", "Before  Calling Send Mail", "", strMethodName);

                                        objCommonBAL = new CommonBAL(_objConfigurationSettingsListDTO, RespectiveConnectionString);
                                        string[] strArrReturn = objCommonBAL.SendMail(objStoreEmailLogListDTO, true, objStoreEmailLogListDTO.EnableSSL);

                                        //General.CreateCodeLog("Step 4.3", "After  Calling Send Mail", "", strMethodName);

                                        if (strArrReturn.Length > 1)
                                        {
                                            objStoreEmailLogListDTO.IsEmailSent = Convert.ToBoolean(strArrReturn[0]);
                                            objStoreEmailLogListDTO.EmailDisclaimer = strArrReturn[1].ToString();
                                            bolValue = objStoreEmailLogListDTO.IsEmailSent;

                                            #region Output

                                            if (bolValue)
                                            {
                                                ErrorCode = 0;
                                            }
                                            #endregion
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        General.objConfigurationSettingsListDTO = _objConfigurationSettingsListDTO;
                                        General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                                    }
                                }
                            }
                            #endregion

                            #region Send Mail Via Multiple Selected itesm
                            else if (!string.IsNullOrEmpty(columnName) && column.IsChildTable)
                            {
                                if (dictChildObject.ContainsKey("ChildData"))
                                {
                                    var childList = dictChildObject["ChildData"] as List<IDictionary<dynamic, dynamic>>;

                                    if (childList != null)
                                    {
                                        foreach (IDictionary<dynamic, dynamic> childItem in childList)
                                        {
                                            if (childItem.ContainsKey(columnName))
                                            {
                                                value = childItem[columnName];
                                                strTableName = column.MasterTableName;
                                                strUsernameColumn = "UserName";

                                                if (dictObject.ContainsKey("AlertTypeId") && dictObject["AlertTypeId"] != null && !string.IsNullOrEmpty(dictObject["AlertTypeId"].ToString()))
                                                {
                                                    alertTypeCode = dictObject["AlertTypeId"].ToString();
                                                }

                                                General.CreateCodeLog("Step 4.4", "Before Calling GetStoreEmailTemplateByCode", "", MethodBase.GetCurrentMethod().Name);
                                                objStoreEmailTemplateBAL = new StoreEmailTemplateBAL(objConfigurationSettingsListDTO);
                                                lstStoreEmailTemplateListDTO = objStoreEmailTemplateBAL.GetStoreEmailTemplateByCode(alertTypeCode, strConnectionString);
                                                General.CreateCodeLog("Step 4.4", "After Calling GetStoreEmailTemplateByCode", "", MethodBase.GetCurrentMethod().Name);

                                                if (lstStoreEmailTemplateListDTO != null && lstStoreEmailTemplateListDTO.Count > 0)
                                                {
                                                    // ===========================
                                                    // This section is for Employee Module
                                                    // ===========================
                                                    // It retrieves the UsernameColumn (e.g., email) for the given Employee record.
                                                    // It builds a dynamic query to get the value from the specified column (usually email or username).
                                                    // Then, it executes the query and fetches the user email (and optionally first name if multiple columns are returned).

                                                    string strQuery = "SELECT " + strUsernameColumn +
                                                                      " FROM " + strTableName +
                                                                      " WHERE " + columnName + " = '" + value + "'";

                                                    objSearchDataDAL = new SearchDataDAL(_objConfigurationSettingsListDTO, RespectiveConnectionString);
                                                    General.CreateCodeLog("Step 4.4", "Before Calling GetDataForEmailDB", "", MethodBase.GetCurrentMethod().Name);
                                                    dtGetSearchData = objSearchDataDAL.GetDataForEmailDB(strQuery);
                                                    if (dtGetSearchData?.Rows.Count > 0)
                                                    {
                                                        strUserEmail = dtGetSearchData.Rows[0][0]?.ToString();
                                                        General.CreateCodeLog("Step 4.5", "After Calling GetDataForEmailDB", "", MethodBase.GetCurrentMethod().Name);

                                                        if (strUsernameColumn.Contains(','))
                                                        {
                                                            strFirstName = (string)dtGetSearchData.Rows[0][1];
                                                        }
                                                        strUserEmail = dtGetSearchData.Rows[0][0]?.ToString();
                                                        strDecryptUserEmail = General.RevalDecrypt(strUserEmail, strAESEncryptkey, strAESEncryptiv);
                                                    }

                                                    if (column.IsAesEncrypt)
                                                    {
                                                        strDecryptUserEmail = General.RevalDecrypt(value, strAESEncryptkey, strAESEncryptiv);
                                                    }

                                                    strEmailContent = lstStoreEmailTemplateListDTO[0].EmailTemplateContent;

                                                    // Get all matches (including duplicates, e.g., [#VenderName#] repeated)
                                                    var matches = Regex.Matches(strEmailContent, @"\[#(.*?)#\]");

                                                    // Convert to a list to avoid issues with modifying the string during iteration
                                                    var placeholderList = matches.Cast<Match>()
                                                                                 .Select(m => new { Full = m.Value, Key = m.Groups[1].Value })
                                                                                 .ToList();

                                                    // Loop through each placeholder one by one
                                                    foreach (var item in placeholderList)
                                                    {
                                                        string placeholder = item.Full;  // [#VenderName#]
                                                        string key = item.Key;           // VenderName

                                                        foreach (var kvp in childItem)
                                                        {
                                                            string dictKey = Convert.ToString(kvp.Key);

                                                            // If key matches (case-insensitive), replace all instances
                                                            if (string.Equals(dictKey, key))
                                                            {
                                                                string OriginalValue = Convert.ToString(kvp.Value);

                                                                // Find column definition
                                                                var columnInfo = lstAPITemplateColumnListDTO
                                                                            .FirstOrDefault(c => string.Equals(c.APITemplateColumnName, key, StringComparison.OrdinalIgnoreCase));

                                                                if (columnInfo != null && columnInfo.IsAesEncrypt)
                                                                {
                                                                    OriginalValue = General.RevalDecrypt(OriginalValue, strAESEncryptkey, strAESEncryptiv);
                                                                }
                                                                strEmailContent = strEmailContent.Replace(placeholder, OriginalValue);
                                                                // break; // done for this match
                                                            }
                                                            else
                                                            {
                                                                strEmailContent = strEmailContent.Replace("[#USERNAME#]", strFirstName);
                                                                strEmailContent = strEmailContent.Replace("[#EmployeeName#]", strFirstName);
                                                                strEmailContent = strEmailContent.Replace("[#ERP URL#]", lstSiteDetails[0].SiteURL);
                                                                strEmailContent = strEmailContent.Replace("[#SiteURL#]", lstSiteDetails[0].SiteURL);
                                                                strEmailContent = strEmailContent.Replace("[#Your User ID#]", strDecryptUserEmail);
                                                                strEmailContent = strEmailContent.Replace("[#Your Password#]", strDecryptPassword);
                                                                strEmailContent = strEmailContent.Replace("[#Logo#]", lstSiteDetails[0].EmailLogo);
                                                                strEmailContent = strEmailContent.Replace("[#CompanyName#]", lstSiteDetails[0].SiteName);
                                                                strEmailContent = strEmailContent.Replace("[#Company Name#]", lstSiteDetails[0].SiteName);
                                                                strEmailContent = strEmailContent.Replace("[#SiteName#]", lstSiteDetails[0].SiteName);
                                                                strEmailContent = strEmailContent.Replace("[#StoreName#]", lstSiteDetails[0].SiteName);
                                                            }
                                                        }
                                                    }

                                                    strEmailSubjet = lstStoreEmailTemplateListDTO[0].EmailTemplateSubject;
                                                    strEmailSubjet = strEmailSubjet.Replace("[#Company Name#]", lstSiteDetails[0].SiteName);

                                                    objStoreEmailLogListDTO = new StoreEmailLogListDTO
                                                    {
                                                        EmailFrom = General.RevalDecrypt(lstStoreEmailTemplateListDTO[0].EmailTemplateFrom, strAESEncryptkey, strAESEncryptiv), //Added by Hanumanthu S on 16 Oct 2024
                                                        EmailSubject = strEmailSubjet,
                                                        StoreEmailTemplateId = lstStoreEmailTemplateListDTO[0].StoreEmailTemplateId,
                                                        SiteId = SiteId,
                                                        EmailContent = strEmailContent,
                                                        EmailTo = strDecryptUserEmail,
                                                        SendEmail = true,
                                                        MailUserName = General.RevalDecrypt(lstStoreEmailTemplateListDTO[0].MailUserName, strAESEncryptkey, strAESEncryptiv), //Added by Hanumanthu S on 16 Oct 2024
                                                        MailPassword = lstStoreEmailTemplateListDTO[0].MailPassword,
                                                        PortNo = lstStoreEmailTemplateListDTO[0].PortNo,
                                                        MailServerName = lstStoreEmailTemplateListDTO[0].MailServerName,
                                                        EnableSSL = lstStoreEmailTemplateListDTO[0].EnableSSL,
                                                        DisplayName = lstStoreEmailTemplateListDTO[0].DisplayName,   // Added By Akhila R on 23 Jan 2024
                                                        AlertTypeId = lstStoreEmailTemplateListDTO[0].AlertTypeId    // Added By Srinivas M on 03 Feb 2024
                                                    };

                                                    try
                                                    {
                                                        //General.CreateCodeLog("Step 4.2", "Before  Calling Send Mail", "", strMethodName);

                                                        objCommonBAL = new CommonBAL(_objConfigurationSettingsListDTO, RespectiveConnectionString);
                                                        string[] strArrReturn = objCommonBAL.SendMail(objStoreEmailLogListDTO, true, objStoreEmailLogListDTO.EnableSSL);

                                                        //General.CreateCodeLog("Step 4.3", "After  Calling Send Mail", "", strMethodName);

                                                        if (strArrReturn.Length > 1)
                                                        {
                                                            objStoreEmailLogListDTO.IsEmailSent = Convert.ToBoolean(strArrReturn[0]);
                                                            objStoreEmailLogListDTO.EmailDisclaimer = strArrReturn[1].ToString();
                                                            bolValue = objStoreEmailLogListDTO.IsEmailSent;

                                                            #region Output

                                                            if (bolValue)
                                                            {
                                                                ErrorCode = 0;
                                                            }
                                                            #endregion
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        General.objConfigurationSettingsListDTO = _objConfigurationSettingsListDTO;
                                                        General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            #endregion

                            #region Send mail via Selected item
                            else if (!string.IsNullOrEmpty(columnName) && dictObject.ContainsKey(columnName) && column.IsMaster)
                            {
                                value = dictObject[columnName];
                                strTableName = column.MasterTableName;
                                strUsernameColumn = "UserName";
                                
                                if (dictObject.ContainsKey("AlertTypeId") && dictObject["AlertTypeId"] != null && !string.IsNullOrEmpty(dictObject["AlertTypeId"].ToString()))
                                {
                                    alertTypeCode = dictObject["AlertTypeId"].ToString();
                                }

                                General.CreateCodeLog("Step 4.4", "Before Calling GetStoreEmailTemplateByCode", "", MethodBase.GetCurrentMethod().Name);
                                objStoreEmailTemplateBAL = new StoreEmailTemplateBAL(objConfigurationSettingsListDTO);
                                lstStoreEmailTemplateListDTO = objStoreEmailTemplateBAL.GetStoreEmailTemplateByCode(alertTypeCode, strConnectionString);
                                General.CreateCodeLog("Step 4.4", "After Calling GetStoreEmailTemplateByCode", "", MethodBase.GetCurrentMethod().Name);

                                if (lstStoreEmailTemplateListDTO != null && lstStoreEmailTemplateListDTO.Count > 0)
                                {
                                    string strQuery = "SELECT " + strUsernameColumn +
                                                    " FROM " + strTableName +
                                                    " WHERE " + columnName + " = '" + value + "'";

                                    objSearchDataDAL = new SearchDataDAL(_objConfigurationSettingsListDTO, RespectiveConnectionString);
                                    General.CreateCodeLog("Step 4.4", "Before Calling GetDataForEmailDB", "", MethodBase.GetCurrentMethod().Name);
                                    dtGetSearchData = objSearchDataDAL.GetDataForEmailDB(strQuery);
                                    if (dtGetSearchData?.Rows.Count > 0)
                                    {
                                        strUserEmail = dtGetSearchData.Rows[0][0]?.ToString();
                                        General.CreateCodeLog("Step 4.5", "After Calling GetDataForEmailDB", "", MethodBase.GetCurrentMethod().Name);

                                        if (strUsernameColumn.Contains(','))
                                        {
                                            strFirstName = (string)dtGetSearchData.Rows[0][1];
                                        }
                                        strUserEmail = dtGetSearchData.Rows[0][0]?.ToString();
                                        strDecryptUserEmail = General.RevalDecrypt(strUserEmail, strAESEncryptkey, strAESEncryptiv);

                                        if (column.IsAesEncrypt)
                                        {
                                            strDecryptUserEmail = General.RevalDecrypt(value, strAESEncryptkey, strAESEncryptiv);
                                        }

                                        strEmailContent = lstStoreEmailTemplateListDTO[0].EmailTemplateContent;

                                        // Get all matches (including duplicates, e.g., [#VenderName#] repeated)
                                        var matches = Regex.Matches(strEmailContent, @"\[#(.*?)#\]");

                                        // Convert to a list to avoid issues with modifying the string during iteration
                                        var placeholderList = matches.Cast<Match>()
                                                                     .Select(m => new { Full = m.Value, Key = m.Groups[1].Value })
                                                                     .ToList();

                                        // Loop through each placeholder one by one
                                        foreach (var item in placeholderList)
                                        {
                                            string placeholder = item.Full;  // [#VenderName#]
                                            string key = item.Key;           // VenderName

                                            foreach (var kvp in dictObject)
                                            {
                                                string dictKey = Convert.ToString(kvp.Key);

                                                // If key matches (case-insensitive), replace all instances
                                                if (string.Equals(dictKey, key))
                                                {
                                                    string OriginalValue = Convert.ToString(kvp.Value);

                                                    // Find column definition
                                                    var columnInfo = lstAPITemplateColumnListDTO
                                                                .FirstOrDefault(c => string.Equals(c.APITemplateColumnName, key, StringComparison.OrdinalIgnoreCase));

                                                    if (columnInfo != null && columnInfo.IsAesEncrypt)
                                                    {
                                                        OriginalValue = General.RevalDecrypt(OriginalValue, strAESEncryptkey, strAESEncryptiv);
                                                    }
                                                    strEmailContent = strEmailContent.Replace(placeholder, OriginalValue);
                                                    // break; // done for this match
                                                }
                                                else
                                                {
                                                    strEmailContent = strEmailContent.Replace("[#USERNAME#]", strFirstName);
                                                    strEmailContent = strEmailContent.Replace("[#EmployeeName#]", strFirstName);
                                                    strEmailContent = strEmailContent.Replace("[#ERP URL#]", lstSiteDetails[0].SiteURL);
                                                    strEmailContent = strEmailContent.Replace("[#SiteURL#]", lstSiteDetails[0].SiteURL);
                                                    strEmailContent = strEmailContent.Replace("[#Your User ID#]", strDecryptUserEmail);
                                                    strEmailContent = strEmailContent.Replace("[#Your Password#]", strDecryptPassword);
                                                    strEmailContent = strEmailContent.Replace("[#Logo#]", lstSiteDetails[0].EmailLogo);
                                                    strEmailContent = strEmailContent.Replace("[#CompanyName#]", lstSiteDetails[0].SiteName);
                                                    strEmailContent = strEmailContent.Replace("[#Company Name#]", lstSiteDetails[0].SiteName);
                                                    strEmailContent = strEmailContent.Replace("[#SiteName#]", lstSiteDetails[0].SiteName);
                                                    strEmailContent = strEmailContent.Replace("[#StoreName#]", lstSiteDetails[0].SiteName);
                                                }
                                            }
                                        }

                                        strEmailSubjet = lstStoreEmailTemplateListDTO[0].EmailTemplateSubject;
                                        strEmailSubjet = strEmailSubjet.Replace("[#Company Name#]", lstSiteDetails[0].SiteName);


                                        objStoreEmailLogListDTO = new StoreEmailLogListDTO
                                        {
                                            EmailFrom = General.RevalDecrypt(lstStoreEmailTemplateListDTO[0].EmailTemplateFrom, strAESEncryptkey, strAESEncryptiv), //Added by Hanumanthu S on 16 Oct 2024
                                            EmailSubject = strEmailSubjet,
                                            StoreEmailTemplateId = lstStoreEmailTemplateListDTO[0].StoreEmailTemplateId,
                                            SiteId = SiteId,
                                            EmailContent = strEmailContent,
                                            EmailTo = strDecryptUserEmail,
                                            SendEmail = true,
                                            MailUserName = General.RevalDecrypt(lstStoreEmailTemplateListDTO[0].MailUserName, strAESEncryptkey, strAESEncryptiv), //Added by Hanumanthu S on 16 Oct 2024
                                            MailPassword = lstStoreEmailTemplateListDTO[0].MailPassword,
                                            PortNo = lstStoreEmailTemplateListDTO[0].PortNo,
                                            MailServerName = lstStoreEmailTemplateListDTO[0].MailServerName,
                                            EnableSSL = lstStoreEmailTemplateListDTO[0].EnableSSL,
                                            DisplayName = lstStoreEmailTemplateListDTO[0].DisplayName,   // Added By Akhila R on 23 Jan 2024
                                            AlertTypeId = lstStoreEmailTemplateListDTO[0].AlertTypeId    // Added By Srinivas M on 03 Feb 2024
                                        };

                                        try
                                        {
                                            //General.CreateCodeLog("Step 4.2", "Before  Calling Send Mail", "", strMethodName);

                                            objCommonBAL = new CommonBAL(_objConfigurationSettingsListDTO, RespectiveConnectionString);
                                            string[] strArrReturn = objCommonBAL.SendMail(objStoreEmailLogListDTO, true, objStoreEmailLogListDTO.EnableSSL);

                                            //General.CreateCodeLog("Step 4.3", "After  Calling Send Mail", "", strMethodName);

                                            if (strArrReturn.Length > 1)
                                            {
                                                objStoreEmailLogListDTO.IsEmailSent = Convert.ToBoolean(strArrReturn[0]);
                                                objStoreEmailLogListDTO.EmailDisclaimer = strArrReturn[1].ToString();
                                                bolValue = objStoreEmailLogListDTO.IsEmailSent;

                                                #region Output

                                                if (bolValue)
                                                {
                                                    ErrorCode = 0;
                                                }
                                                #endregion
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            General.objConfigurationSettingsListDTO = _objConfigurationSettingsListDTO;
                                            General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                                        }
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                General.objConfigurationSettingsListDTO = _objConfigurationSettingsListDTO;
                ErrorCode = Convert.ToInt32(General.ErrorCode.Technical_Error_occured);
                General.CreateErrorLog(ex, strMethodName, lstSiteDetails, lstSiteSolrURlListDTO);
            }

        }





















        public void sendSMSAlert(IDictionary<dynamic, dynamic> dictObject, List<APITemplateColumnListDTO> lstAPITemplateColumnListDTO, string strConnectionString, string Key, int LogTypeId, int CodeLogRequired, List<SiteDetails> lstSiteDetails, DataTable dtResult)
        {
            int intEmailTemplateId = 0, ErrorCode = 0;
            APITemplateDAL objAPITemplateDAL = null;
            dynamic value = null;
            SMSTemplateBAL objSMSTemplateBAL = null;
            string columnName = string.Empty;
            string alertTypeCode = string.Empty;
            List<StoreEmailTemplateListDTO> lstStoreEmailTemplateListDTO = null;
            StoreEmailLogListDTO objStoreEmailLogListDTO = null;
            string strAESEncryptkey = string.Empty;
            string strAESEncryptiv = string.Empty;
            string strEmailContent = string.Empty;
            string strUserMobile = string.Empty;
            ErrorCodeBAL objErrorCodeBAL = null;   //Added by Manisha on 24 July 2024
            List<ErrorCodeListDTO> lstErrorCodeListDTO = null;
            string strTableName = string.Empty;
            string strUsernameColumn = string.Empty;
            SearchDataDAL objSearchDataDAL = null;
            DataTable dtGetSearchData = null;
            string strFirstName = string.Empty;
            string strDecryptUserMobile = string.Empty, strDecryptPassword = string.Empty;
            string strEmailSubjet = string.Empty;
            string strPassword = string.Empty, strMethodName = MethodBase.GetCurrentMethod().Name, strCreatedBy = string.Empty;
            List<APITemplateColumnListDTO> lstPasswordAPITemplateColumnListDTO = null;
            CommonBAL objCommonBAL = null;
            bool bolValue = false, IsCredentialsSendMail = false;

            List<SMSTemplateListDTO> lstSmsTemplateListDTO = null;


            try
            {
                #region Encryptionkey 
                if (ErrorCode == 0)
                {
                    if (!string.IsNullOrEmpty(_objConfigurationSettingsListDTO.AESEncryptkey))
                    {
                        strAESEncryptkey = clsCryptography.Decrypt(_objConfigurationSettingsListDTO.AESEncryptkey, _objConfigurationSettingsListDTO.EncryptionKey);
                    }
                    if (!string.IsNullOrEmpty(_objConfigurationSettingsListDTO.AESEncryptiv))
                    {
                        strAESEncryptiv = clsCryptography.Decrypt(_objConfigurationSettingsListDTO.AESEncryptiv, _objConfigurationSettingsListDTO.EncryptionKey);
                    }
                }
                #endregion


                if (lstAPITemplateColumnListDTO != null && lstAPITemplateColumnListDTO.Count > 0)
                {

                    if (dictObject == null || lstAPITemplateColumnListDTO == null)
                    {
                        Console.WriteLine("One or both inputs are null.");
                        return;
                    }

                    LoginUserName = dictObject["CreatedBy"]?.ToString();

                    foreach (APITemplateColumnListDTO column1 in lstAPITemplateColumnListDTO)
                    {
                        string columnName1 = column1.APITemplateColumnName;
                        if (column1.APITemplateColumnName.Contains("Password"))
                        {
                            strPassword = dictObject[columnName1];
                            strDecryptPassword = clsCryptography.Decrypt(strPassword);
                        }
                    }

                    foreach (APITemplateColumnListDTO column in lstAPITemplateColumnListDTO)
                    {
                        columnName = column.APITemplateColumnName;
                        
                        lstPasswordAPITemplateColumnListDTO = lstAPITemplateColumnListDTO.Where(obj => obj.IsPassword == true && obj.IsPasswordAutoGenerate == true).ToList();
                        foreach (var objPasswordAPIColumnListDTO in lstPasswordAPITemplateColumnListDTO)
                        {
                            strUsernameColumn = objPasswordAPIColumnListDTO.UsernameColumn;
                        }

                        if (column.IsSMSEnabled)
                        {

                            alertTypeCode = column.AlertTypeCode;
                            strTableName = column.TableName;

                            if (!string.IsNullOrEmpty(columnName) && dictObject.ContainsKey(columnName))
                            {
                                value = dictObject[columnName];

                                General.CreateCodeLog("Step 4.4", "Before Calling GetStoreEmailTemplateByCode", "", MethodBase.GetCurrentMethod().Name);
                                objSMSTemplateBAL = new SMSTemplateBAL(_objConfigurationSettingsListDTO);
                                lstSmsTemplateListDTO = objSMSTemplateBAL.GetSMSTemplateByCode(alertTypeCode, strConnectionString);
                                General.CreateCodeLog("Step 4.4", "After Calling GetStoreEmailTemplateByCode", "", MethodBase.GetCurrentMethod().Name);
                            }

                            // ===========================
                            // This section is for Employee Module
                            // ===========================
                            // It retrieves the UsernameColumn (e.g., email) for the given Employee record.
                            // It builds a dynamic query to get the value from the specified column (usually email or username).
                            // Then, it executes the query and fetches the user email (and optionally first name if multiple columns are returned).

                            if (!string.IsNullOrEmpty(strUsernameColumn))
                            {
                                string strCode = (string)dtResult.Rows[0]["Id"];
                                string strQuery = "SELECT " + strUsernameColumn + " FROM " + strTableName + " WHERE ID ='" + strCode + "'";

                                objSearchDataDAL = new SearchDataDAL(_objConfigurationSettingsListDTO, RespectiveConnectionString);
                                General.CreateCodeLog("Step 4.4", "Before Calling GetDataForEmailDB", "", MethodBase.GetCurrentMethod().Name);
                                dtGetSearchData = objSearchDataDAL.GetDataForEmailDB(strQuery);
                                strUserMobile = dtGetSearchData.Rows[0][2]?.ToString();
                                General.CreateCodeLog("Step 4.5", "After Calling GetDataForEmailDB", "", MethodBase.GetCurrentMethod().Name);

                                if (strUsernameColumn.Contains(','))
                                {
                                    strFirstName = (string)dtGetSearchData.Rows[0][1];
                                }
                                // strUserMobile = dtGetSearchData.Rows[0][2]?.ToString();
                                strDecryptUserMobile = General.RevalDecrypt(strUserMobile, strAESEncryptkey, strAESEncryptiv);
                            }

                            if (column.IsAesEncrypt)
                            {
                                strDecryptUserMobile = General.RevalDecrypt(value, strAESEncryptkey, strAESEncryptiv);
                            }
                            if (lstSmsTemplateListDTO != null && lstSmsTemplateListDTO.Count > 0)
                            {
                                string strSMSTemplateContent = lstSmsTemplateListDTO[0].SMSAlertContent;
                                string SMSTemplateName = lstSmsTemplateListDTO[0].SMSTemplateName;
                               // strEmailLogo = objConfigurationSettingsListDTO.EmailLogo;
                                string strEmailSiteURL = lstSiteDetails[0].SiteURL;

                                // Match all placeholders like [#SomeKey#]
                                var matches = Regex.Matches(strSMSTemplateContent, @"\[#(.*?)#\]");
                                var placeholderList = matches.Cast<Match>()
                                                             .Select(m => new { Full = m.Value, Key = m.Groups[1].Value })
                                                             .ToList();

                                foreach (var item in placeholderList)
                                {
                                    string placeholder = item.Full;
                                    string key = item.Key;

                                   

                                    foreach (var kvp in dictObject)
                                    {
                                        
                                        string dictKey = Convert.ToString(kvp.Key);

                                        if (string.Equals(dictKey, key, StringComparison.OrdinalIgnoreCase))
                                        {
                                            string OriginalValue = Convert.ToString(kvp.Value);

                                            // Find column definition
                                            var columnInfo = lstAPITemplateColumnListDTO
                                                        .FirstOrDefault(c => string.Equals(c.APITemplateColumnName, key, StringComparison.OrdinalIgnoreCase));

                                            // If AES encryption is enabled for this column, decrypt
                                            if (columnInfo != null && columnInfo.IsAesEncrypt)
                                            {
                                                OriginalValue = General.RevalDecrypt(OriginalValue, strAESEncryptkey, strAESEncryptiv);
                                            }

                                            strSMSTemplateContent = strSMSTemplateContent.Replace(placeholder, OriginalValue);
                                            //break;
                                        }
                                        else
                                        {
                                            strSMSTemplateContent = strSMSTemplateContent.Replace("[#USERNAME#]", strFirstName);
                                            strSMSTemplateContent = strSMSTemplateContent.Replace("[#EmployeeName#]", strFirstName);
                                            strSMSTemplateContent = strSMSTemplateContent.Replace("[#ERP URL#]", lstSiteDetails[0].SiteURL);
                                            strSMSTemplateContent = strSMSTemplateContent.Replace("[#SiteURL#]", lstSiteDetails[0].SiteURL);
                                            strSMSTemplateContent = strSMSTemplateContent.Replace("[#Your User ID#]", strDecryptUserMobile);
                                            strSMSTemplateContent = strSMSTemplateContent.Replace("[#Your Password#]", strDecryptPassword);
                                            strSMSTemplateContent = strSMSTemplateContent.Replace("[#Company Name#]", lstSiteDetails[0].SiteName);
                                            strSMSTemplateContent = strSMSTemplateContent.Replace("[#SiteName#]", lstSiteDetails[0].SiteName);
                                            strSMSTemplateContent = strSMSTemplateContent.Replace("[#StoreName#]", lstSiteDetails[0].SiteName);
                                        }
                                    }
                                }




                                try
                                {
                                    General.CreateCodeLog("Step 4.7", "Before Calling SendSms", "", MethodBase.GetCurrentMethod().Name);
                                    objCommonBAL = new CommonBAL(_objConfigurationSettingsListDTO, RespectiveConnectionString);
                                    bool IsSMSSent = objCommonBAL.SendSms(lstSmsTemplateListDTO[0], strDecryptUserMobile, strSMSTemplateContent, TimeZone, NoOfHours, NoOfMinutes, LoginUserName);
                                    General.CreateCodeLog("Step 4.8", "After Calling SendSms", "", MethodBase.GetCurrentMethod().Name);

                                    if (IsSMSSent)
                                    {
                                        ErrorCode = 0;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                                }

                            }
                        }


                    }
                }
            }
            catch (Exception ex)
            {
                General.objConfigurationSettingsListDTO = _objConfigurationSettingsListDTO;
                General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
            }
        }


        public void sendWhatsAppAlert(IDictionary<dynamic, dynamic> dictObject, List<APITemplateColumnListDTO> lstAPITemplateColumnListDTO, string strConnectionString, string Key, int LogTypeId, int CodeLogRequired, List<SiteDetails> lstSiteDetails, DataTable dtResult)
        {
            int intEmailTemplateId = 0, ErrorCode = 0;
            APITemplateDAL objAPITemplateDAL = null;
            dynamic value = null;
            WhatsAppTemplateBAL objWhatsAppTemplateBAL = null;
            string columnName = string.Empty;
            string alertTypeCode = string.Empty;
            string strAESEncryptkey = string.Empty;
            string strAESEncryptiv = string.Empty;
            string strEmailContent = string.Empty;
            string strUserMobile = string.Empty;
            ErrorCodeBAL objErrorCodeBAL = null;   //Added by Manisha on 24 July 2024
            List<ErrorCodeListDTO> lstErrorCodeListDTO = null;
            string strTableName = string.Empty;
            string strUsernameColumn = string.Empty;
            SearchDataDAL objSearchDataDAL = null;
            DataTable dtGetSearchData = null;
            string strFirstName = string.Empty;
            string strDecryptUserMobile = string.Empty, strDecryptPassword = string.Empty;
            string strEmailSubjet = string.Empty;
            string strPassword = string.Empty, strMethodName = MethodBase.GetCurrentMethod().Name, strCreatedBy = string.Empty;
            List<APITemplateColumnListDTO> lstPasswordAPITemplateColumnListDTO = null;
            CommonBAL objCommonBAL = null;
            bool bolValue = false, IsCredentialsSendMail = false;
            WhatsAppAlert objWhatsAppAlert = null;
            List<WhatsAppNotificationTemplateListDTO> lstWhatsAppNotificationTemplateListDTO = null;
            List<WhatsAppAttributeListDTO> list2 = null;
            WhatsAppServicesListDTO whatsAppServicesListDTO = null;
            WhatsAppAttributeListDTO whatsAppAttributeListDTO = null;
            WhatsAppTemplateBAL objwhatsAppTemplateBAL = null;
            string empty = string.Empty;
            string strWhatsAppTemplateContent = string.Empty;
            string WhatsAppTemplateName = string.Empty;

            try
            {
                #region Encryptionkey 
                if (ErrorCode == 0)
                {
                    if (!string.IsNullOrEmpty(_objConfigurationSettingsListDTO.AESEncryptkey))
                    {
                        strAESEncryptkey = clsCryptography.Decrypt(_objConfigurationSettingsListDTO.AESEncryptkey, _objConfigurationSettingsListDTO.EncryptionKey);
                    }
                    if (!string.IsNullOrEmpty(_objConfigurationSettingsListDTO.AESEncryptiv))
                    {
                        strAESEncryptiv = clsCryptography.Decrypt(_objConfigurationSettingsListDTO.AESEncryptiv, _objConfigurationSettingsListDTO.EncryptionKey);
                    }
                }
                #endregion


                if (lstAPITemplateColumnListDTO != null && lstAPITemplateColumnListDTO.Count > 0)
                {

                    if (dictObject == null || lstAPITemplateColumnListDTO == null)
                    {
                        Console.WriteLine("One or both inputs are null.");
                        return;
                    }

                    LoginUserName = dictObject["CreatedBy"]?.ToString();

                    foreach (APITemplateColumnListDTO column1 in lstAPITemplateColumnListDTO)
                    {
                        string columnName1 = column1.APITemplateColumnName;
                        if (column1.APITemplateColumnName.Contains("Password"))
                        {
                            strPassword = dictObject[columnName1];
                            strDecryptPassword = clsCryptography.Decrypt(strPassword);
                        }
                    }

                    foreach (APITemplateColumnListDTO column in lstAPITemplateColumnListDTO)
                    {
                        columnName = column.APITemplateColumnName;

                        lstPasswordAPITemplateColumnListDTO = lstAPITemplateColumnListDTO.Where(obj => obj.IsPassword == true && obj.IsPasswordAutoGenerate == true).ToList();
                        foreach (var objPasswordAPIColumnListDTO in lstPasswordAPITemplateColumnListDTO)
                        {
                            strUsernameColumn = objPasswordAPIColumnListDTO.UsernameColumn;
                        }

                        if (column.IsWhatsAppEnabled)
                        {

                            alertTypeCode = column.AlertTypeCode;
                            strTableName = column.TableName;

                            if (!string.IsNullOrEmpty(columnName) && dictObject.ContainsKey(columnName))
                            {
                                value = dictObject[columnName];

                                General.CreateCodeLog("Step 4.4", "Before Calling GetStoreEmailTemplateByCode", "", MethodBase.GetCurrentMethod().Name);
                                objWhatsAppTemplateBAL = new WhatsAppTemplateBAL(RespectiveConnectionString, _objConfigurationSettingsListDTO.EncryptionKey, _objConfigurationSettingsListDTO);
                                lstWhatsAppNotificationTemplateListDTO = objWhatsAppTemplateBAL.GetWhatsAppTemplateDataByAlertTypeCode(alertTypeCode);
                                General.CreateCodeLog("Step 4.4", "After Calling GetStoreEmailTemplateByCode", "", MethodBase.GetCurrentMethod().Name);
                            }

                            // ===========================
                            // This section is for Employee Module
                            // ===========================
                            // It retrieves the UsernameColumn (e.g., email) for the given Employee record.
                            // It builds a dynamic query to get the value from the specified column (usually email or username).
                            // Then, it executes the query and fetches the user email (and optionally first name if multiple columns are returned).

                            if (!string.IsNullOrEmpty(strUsernameColumn))
                            {
                                string strCode = (string)dtResult.Rows[0]["Id"];
                                string strQuery = "SELECT " + strUsernameColumn + " FROM " + strTableName + " WHERE ID ='" + strCode + "'";

                                objSearchDataDAL = new SearchDataDAL(_objConfigurationSettingsListDTO, RespectiveConnectionString);
                                General.CreateCodeLog("Step 4.4", "Before Calling GetDataForEmailDB", "", MethodBase.GetCurrentMethod().Name);
                                dtGetSearchData = objSearchDataDAL.GetDataForEmailDB(strQuery);
                                strUserMobile = dtGetSearchData.Rows[0][2]?.ToString();
                                General.CreateCodeLog("Step 4.5", "After Calling GetDataForEmailDB", "", MethodBase.GetCurrentMethod().Name);

                                if (strUsernameColumn.Contains(','))
                                {
                                    strFirstName = (string)dtGetSearchData.Rows[0][1];
                                }
                                // strUserMobile = dtGetSearchData.Rows[0][2]?.ToString();
                                strDecryptUserMobile = General.RevalDecrypt(strUserMobile, strAESEncryptkey, strAESEncryptiv);
                            }
                            if(column.IsAesEncrypt)
                            {
                                strDecryptUserMobile = General.RevalDecrypt(value, strAESEncryptkey, strAESEncryptiv);
                            }

                            if(lstWhatsAppNotificationTemplateListDTO != null && lstWhatsAppNotificationTemplateListDTO.Count >0)
                            {
                                 strWhatsAppTemplateContent = lstWhatsAppNotificationTemplateListDTO[0].TemplateContent;
                                 WhatsAppTemplateName = lstWhatsAppNotificationTemplateListDTO[0].WhatsAppTemplateName;
                                string strEmailSiteURL = lstSiteDetails[0].SiteURL;

                                // Match all placeholders like [#SomeKey#]
                                var matches = Regex.Matches(strWhatsAppTemplateContent, @"\[#(.*?)#\]");
                                var placeholderList = matches.Cast<Match>()
                                                             .Select(m => new { Full = m.Value, Key = m.Groups[1].Value })
                                                             .ToList();
                                foreach (var item in placeholderList)
                                {
                                    string placeholder = item.Full;
                                    string key = item.Key;

                                    foreach (var kvp in dictObject)
                                    {
                                        string dictKey = Convert.ToString(kvp.Key);

                                        if (string.Equals(dictKey, key, StringComparison.OrdinalIgnoreCase))
                                        {
                                            string OriginalValue = Convert.ToString(kvp.Value);

                                            // Find column definition
                                            var columnInfo = lstAPITemplateColumnListDTO
                                                        .FirstOrDefault(c => string.Equals(c.APITemplateColumnName, key, StringComparison.OrdinalIgnoreCase));

                                            // If AES encryption is enabled for this column, decrypt
                                            if (columnInfo != null && columnInfo.IsAesEncrypt)
                                            {
                                                OriginalValue = General.RevalDecrypt(OriginalValue, strAESEncryptkey, strAESEncryptiv);
                                            }

                                            strWhatsAppTemplateContent = strWhatsAppTemplateContent.Replace(placeholder, OriginalValue);
                                            //break;
                                        }
                                        else
                                        {
                                            strWhatsAppTemplateContent = strWhatsAppTemplateContent.Replace("[#USERNAME#]", strFirstName);
                                            strWhatsAppTemplateContent = strWhatsAppTemplateContent.Replace("[#EmployeeName#]", strFirstName);
                                            strWhatsAppTemplateContent = strWhatsAppTemplateContent.Replace("[#ERP URL#]", lstSiteDetails[0].SiteURL);
                                            strWhatsAppTemplateContent = strWhatsAppTemplateContent.Replace("[#SiteURL#]", lstSiteDetails[0].SiteURL);
                                            strWhatsAppTemplateContent = strWhatsAppTemplateContent.Replace("[#YourUserID#]", strDecryptUserMobile);
                                            strWhatsAppTemplateContent = strWhatsAppTemplateContent.Replace("[#YourPassword#]", strDecryptPassword);
                                            strWhatsAppTemplateContent = strWhatsAppTemplateContent.Replace("[#Company Name#]", lstSiteDetails[0].SiteName);
                                            strWhatsAppTemplateContent = strWhatsAppTemplateContent.Replace("[#SiteName#]", lstSiteDetails[0].SiteName);
                                            strWhatsAppTemplateContent = strWhatsAppTemplateContent.Replace("[#StoreName#]", lstSiteDetails[0].SiteName);
                                        }
                                    }
                                }


                                }
                           // strDecryptUserMobile = General.RevalDecrypt(strUserMobile, strAESEncryptkey, strAESEncryptiv);

                            #region SendWhatsapp
                            if (!string.IsNullOrEmpty(strDecryptUserMobile))
                            {
                                WhatsAppAlert objWhatsAppAlert1 = null;
                                try
                                {
                                    int alertTypeId = lstWhatsAppNotificationTemplateListDTO
                                            .FirstOrDefault()?.AlertTypeId ?? 0;
                                    General.CreateCodeLog("Step 4.9", "Before Calling objWhatsappSiteListDTO", "", MethodBase.GetCurrentMethod().Name);

                                    Revalsys.Whatsapp.Properties.SiteListDTO objWhatsappSiteListDTO = new Revalsys.Whatsapp.Properties.SiteListDTO
                                    {
                                        ConnectionString = RespectiveConnectionString,
                                        EncryptionKey = _objConfigurationSettingsListDTO.EncryptionKey,
                                        SiteId = _objConfigurationSettingsListDTO.SiteId
                                    };
                                    General.CreateCodeLog("Step 5.0", "After Calling objWhatsappSiteListDTO", "", MethodBase.GetCurrentMethod().Name);



                                    Revalsys.Whatsapp.Properties.WhatsAppServicesListDTO objWhatsAppServiceDTO = new Revalsys.Whatsapp.Properties.WhatsAppServicesListDTO
                                    {
                                        CompanyId = _objConfigurationSettingsListDTO.CompanyId,
                                        SiteId = lstSiteDetails[0].SiteId,
                                        DateCreated = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes),
                                        FromDate = RevalTimeZone.GetDateByTimezone(DateTime.Now, TimeZone, NoOfHours, NoOfMinutes),
                                        PublishedBy = LoginUserName,
                                        AlertTypeId = alertTypeId,
                                        IPAddress = _objConfigurationSettingsListDTO.IPAddress,
                                        WhatsappTitle = WhatsAppTemplateName,
                                        Mobile = value,
                                        CreatedBy = LoginUserName,
                                        UserName = LoginUserName,
                                        CompanyName = SiteName,
                                        EmployeeName = strFirstName,
                                        SiteURL = lstSiteDetails[0].SiteURL,
                                        CustomerPassword = strDecryptPassword,
                                        SiteName = SiteName,
                                        WhatsAppTemplateId = lstWhatsAppNotificationTemplateListDTO[0].WhatsAppTemplateId,
                                        WhatsAppProviderId = lstWhatsAppNotificationTemplateListDTO[0].WhatsAppProviderId,
                                        Password = lstWhatsAppNotificationTemplateListDTO[0].Password,
                                        Request = lstWhatsAppNotificationTemplateListDTO[0].Request,
                                        MaskedMobileNo = General.MaskDemographicDetail(strDecryptUserMobile),
                                        Message = strWhatsAppTemplateContent,
                                        DatePublished = DateTime.Now,
                                        IsPublished = true,
                                        DisplayOnWeb = true,

                                    };
                                    string templateContent2 = lstWhatsAppNotificationTemplateListDTO[0].TemplateContent;
                                    //whatsAppServicesListDTO.APIURL = empty;
                                    //whatsAppAttributeListDTO = new WhatsAppAttributeListDTO();
                                    //list2 = new List<WhatsAppAttributeListDTO>();
                                    if (objWhatsAppServiceDTO != null)
                                    {
                                        objwhatsAppTemplateBAL = new WhatsAppTemplateBAL(RespectiveConnectionString, _objConfigurationSettingsListDTO.EncryptionKey, _objConfigurationSettingsListDTO);
                                        objwhatsAppTemplateBAL.InsertWhatsAppService(objWhatsAppServiceDTO);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    General.objConfigurationSettingsListDTO = _objConfigurationSettingsListDTO;
                                    General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
                                }
                            }
                            #endregion SendWhatsapp

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                General.objConfigurationSettingsListDTO = _objConfigurationSettingsListDTO;
                General.CreateErrorLog(ex, MethodBase.GetCurrentMethod().Name, lstSiteDetails, lstSiteSolrURlListDTO);
            }
        }













