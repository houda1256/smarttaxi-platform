using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SmartTaxi.API.Endpoints.Administration;
using SmartTaxi.API.Endpoints.Advertising;
using SmartTaxi.API.Endpoints.Analytics;
using SmartTaxi.API.Endpoints.Fleet;
using SmartTaxi.API.Endpoints.Identity;
using SmartTaxi.API.Endpoints.Loyalty;
using SmartTaxi.API.Endpoints.Maintenance;
using SmartTaxi.API.Endpoints.RoadsideAssistance;
using SmartTaxi.API.Endpoints.Support;
using SmartTaxi.API.Endpoints.Notifications;
using SmartTaxi.API.Endpoints.Payments;
using SmartTaxi.API.Endpoints.Rides;
using SmartTaxi.API.Endpoints.Subscriptions;
using SmartTaxi.API.ErrorHandling;
using SmartTaxi.API.Realtime;
using SmartTaxi.Application.Advertising.Commands.ActivatePlacement;
using SmartTaxi.Application.Advertising.Commands.ApproveCampaign;
using SmartTaxi.Application.Advertising.Commands.CancelCampaign;
using SmartTaxi.Application.Advertising.Commands.CreateCampaign;
using SmartTaxi.Application.Advertising.Commands.CreatePlacement;
using SmartTaxi.Application.Advertising.Commands.DeactivatePlacement;
using SmartTaxi.Application.Advertising.Commands.PauseCampaign;
using SmartTaxi.Application.Advertising.Commands.ProcessCompletedCampaigns;
using SmartTaxi.Application.Advertising.Commands.ProcessScheduledCampaigns;
using SmartTaxi.Application.Advertising.Commands.ReactivateCampaign;
using SmartTaxi.Application.Advertising.Commands.RecordClick;
using SmartTaxi.Application.Advertising.Commands.RecordImpression;
using SmartTaxi.Application.Advertising.Commands.RegisterAdvertiserProfile;
using SmartTaxi.Application.Advertising.Commands.RejectCampaign;
using SmartTaxi.Application.Advertising.Commands.RequestAdDelivery;
using SmartTaxi.Application.Advertising.Commands.RequestCampaignChanges;
using SmartTaxi.Application.Advertising.Commands.ResumeCampaign;
using SmartTaxi.Application.Advertising.Commands.SettleCampaignBudget;
using SmartTaxi.Application.Advertising.Commands.SubmitCampaign;
using SmartTaxi.Application.Advertising.Commands.SuspendCampaign;
using SmartTaxi.Application.Advertising.Commands.UpdateAdvertiserProfile;
using SmartTaxi.Application.Advertising.Commands.UpdateCampaign;
using SmartTaxi.Application.Advertising.Commands.UpdatePlacement;
using SmartTaxi.Application.Advertising.Commands.UploadCampaignMedia;
using SmartTaxi.Application.Advertising.Queries.GetCampaignDetails;
using SmartTaxi.Application.Advertising.Queries.GetCampaignDetailsAdmin;
using SmartTaxi.Application.Advertising.Queries.GetCampaignMediaContent;
using SmartTaxi.Application.Advertising.Queries.GetCampaignPerformance;
using SmartTaxi.Application.Advertising.Queries.GetCampaignPerformanceAdmin;
using SmartTaxi.Application.Advertising.Queries.GetMyAdvertiserProfile;
using SmartTaxi.Application.Advertising.Queries.GetMyCampaigns;
using SmartTaxi.Application.Advertising.Queries.GetPendingReviewCampaigns;
using SmartTaxi.Application.Advertising.Queries.GetPlacements;
using SmartTaxi.Application.Advertising.Queries.GetPlacementsAdmin;
using SmartTaxi.Application.Fleet.Alerts.Commands.DismissFleetAlert;
using SmartTaxi.Application.Fleet.Alerts.Commands.GenerateFleetAlerts;
using SmartTaxi.Application.Fleet.Alerts.Commands.ResolveFleetAlert;
using SmartTaxi.Application.Fleet.Alerts.Queries.GetFleetAlerts;
using SmartTaxi.Application.Fleet.Assignments.Commands.ActivateAssignment;
using SmartTaxi.Application.Fleet.Assignments.Commands.ApproveAssignment;
using SmartTaxi.Application.Fleet.Assignments.Commands.CancelAssignment;
using SmartTaxi.Application.Fleet.Assignments.Commands.CompleteAssignment;
using SmartTaxi.Application.Fleet.Assignments.Commands.CreateAssignment;
using SmartTaxi.Application.Fleet.Assignments.Commands.SuspendAssignment;
using SmartTaxi.Application.Fleet.Assignments.Queries.GetDriverAssignments;
using SmartTaxi.Application.Fleet.Assignments.Queries.GetVehicleAssignments;
using SmartTaxi.Application.Fleet.Assignments.Queries.ValidateAssignmentAvailability;
using SmartTaxi.Application.Fleet.Contracts.Commands.ActivateContract;
using SmartTaxi.Application.Fleet.Contracts.Commands.CreateContract;
using SmartTaxi.Application.Fleet.Contracts.Commands.SubmitContract;
using SmartTaxi.Application.Fleet.Contracts.Commands.SuspendContract;
using SmartTaxi.Application.Fleet.Contracts.Commands.TerminateContract;
using SmartTaxi.Application.Fleet.Contracts.Commands.UpdateDraftContract;
using SmartTaxi.Application.Fleet.Contracts.Queries.GetActiveContract;
using SmartTaxi.Application.Fleet.Contracts.Queries.GetContractsForDriver;
using SmartTaxi.Application.Fleet.Contracts.Queries.GetContractsForOwner;
using SmartTaxi.Application.Fleet.Drivers.Commands.ApproveDriver;
using SmartTaxi.Application.Fleet.Drivers.Commands.CreateDriverProfile;
using SmartTaxi.Application.Fleet.Drivers.Commands.RejectDriver;
using SmartTaxi.Application.Fleet.Drivers.Commands.SetDriverAvailability;
using SmartTaxi.Application.Fleet.Drivers.Commands.SubmitDriverForReview;
using SmartTaxi.Application.Fleet.Drivers.Commands.SuspendDriver;
using SmartTaxi.Application.Fleet.Drivers.Commands.UpdateDriverProfile;
using SmartTaxi.Application.Fleet.Drivers.Queries.GetDriverProfileById;
using SmartTaxi.Application.Fleet.Drivers.Queries.GetEligibleDrivers;
using SmartTaxi.Application.Fleet.Drivers.Queries.GetMyDriverProfile;
using SmartTaxi.Application.Fleet.Expenses.Commands.ApproveExpense;
using SmartTaxi.Application.Fleet.Expenses.Commands.CreateExpense;
using SmartTaxi.Application.Fleet.Expenses.Commands.MarkExpensePaid;
using SmartTaxi.Application.Fleet.Expenses.Commands.RejectExpense;
using SmartTaxi.Application.Fleet.Expenses.Commands.SubmitExpense;
using SmartTaxi.Application.Fleet.Expenses.Queries.ListExpenses;
using SmartTaxi.Application.Fleet.Fleets.Commands.AddFleetCollaborator;
using SmartTaxi.Application.Fleet.Fleets.Commands.CreateFleet;
using SmartTaxi.Application.Fleet.Fleets.Commands.RemoveFleetCollaborator;
using SmartTaxi.Application.Fleet.Fleets.Commands.SuspendFleet;
using SmartTaxi.Application.Fleet.Fleets.Commands.UpdateFleet;
using SmartTaxi.Application.Fleet.Fleets.Queries.GetFleetById;
using SmartTaxi.Application.Fleet.Fleets.Queries.GetFleetCollaborators;
using SmartTaxi.Application.Fleet.Fleets.Queries.GetMyFleets;
using SmartTaxi.Application.Fleet.Owners.Commands.CreateCompanyOwnerProfile;
using SmartTaxi.Application.Fleet.Owners.Commands.CreateIndividualOwnerProfile;
using SmartTaxi.Application.Fleet.Owners.Commands.UpdateOwnerProfile;
using SmartTaxi.Application.Fleet.Owners.Queries.GetMyOwnerProfile;
using SmartTaxi.Application.Fleet.UsageHistory.Queries.GetDriverUsageHistory;
using SmartTaxi.Application.Fleet.UsageHistory.Queries.GetVehicleUsageHistory;
using SmartTaxi.Application.Fleet.Vehicles.Commands.ApproveVehicle;
using SmartTaxi.Application.Fleet.Vehicles.Commands.RegisterVehicle;
using SmartTaxi.Application.Fleet.Vehicles.Commands.RejectVehicle;
using SmartTaxi.Application.Fleet.Vehicles.Commands.RetireVehicle;
using SmartTaxi.Application.Fleet.Vehicles.Commands.SubmitVehicleForVerification;
using SmartTaxi.Application.Fleet.Vehicles.Commands.SuspendVehicle;
using SmartTaxi.Application.Fleet.Vehicles.Commands.UpdateVehicle;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.ApproveVehicleDocument;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.ExpireVehicleDocuments;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.RejectVehicleDocument;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.ReplaceVehicleDocument;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.SuspendVehicleDocument;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.UploadVehicleDocument;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Queries.GetPendingVehicleDocuments;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Queries.GetVehicleDocumentContent;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Queries.GetVehicleDocumentContentAdmin;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Queries.GetVehicleDocuments;
using SmartTaxi.Application.Fleet.Vehicles.Queries.GetFleetVehicles;
using SmartTaxi.Application.Fleet.Vehicles.Queries.GetOwnerVehicles;
using SmartTaxi.Application.Fleet.Vehicles.Queries.GetVehicleById;
using SmartTaxi.Application.Fleet.Vehicles.Queries.GetVehicleEligibility;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Identity.Commands.AssignRole;
using SmartTaxi.Application.Identity.Commands.ConfirmEmailVerification;
using SmartTaxi.Application.Identity.Commands.LoginUser;
using SmartTaxi.Application.Identity.Commands.Logout;
using SmartTaxi.Application.Identity.Commands.RefreshToken;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Commands.RemoveRole;
using SmartTaxi.Application.Identity.Commands.ChangePassword;
using SmartTaxi.Application.Identity.Commands.ConfirmPhoneVerification;
using SmartTaxi.Application.Identity.Commands.ConfirmTwoFactor;
using SmartTaxi.Application.Identity.Commands.DisableTwoFactor;
using SmartTaxi.Application.Identity.Commands.EnrollTwoFactor;
using SmartTaxi.Application.Identity.Commands.ForgotPassword;
using SmartTaxi.Application.Identity.Documents.Commands.ApproveDocument;
using SmartTaxi.Application.Identity.Documents.Commands.CancelDocument;
using SmartTaxi.Application.Identity.Documents.Commands.ExpireDocuments;
using SmartTaxi.Application.Identity.Documents.Commands.RejectDocument;
using SmartTaxi.Application.Identity.Documents.Commands.ReplaceDocument;
using SmartTaxi.Application.Identity.Documents.Commands.SuspendDocument;
using SmartTaxi.Application.Identity.Documents.Commands.UploadDocument;
using SmartTaxi.Application.Identity.Documents.Queries.GetDocumentByIdAdmin;
using SmartTaxi.Application.Identity.Documents.Queries.GetDocumentContentAdmin;
using SmartTaxi.Application.Identity.Documents.Queries.GetMyDocumentById;
using SmartTaxi.Application.Identity.Documents.Queries.GetMyDocumentContent;
using SmartTaxi.Application.Identity.Documents.Queries.GetMyDocuments;
using SmartTaxi.Application.Identity.Documents.Queries.GetPendingDocuments;
using SmartTaxi.Application.Identity.Documents.Queries.GetUserProfessionalEligibility;
using SmartTaxi.Application.Identity.Commands.RegenerateRecoveryCodes;
using SmartTaxi.Application.Identity.Commands.RequestEmailVerification;
using SmartTaxi.Application.Identity.Commands.RequestPhoneVerification;
using SmartTaxi.Application.Identity.Commands.ResetPassword;
using SmartTaxi.Application.Identity.Commands.RevokeAllSessions;
using SmartTaxi.Application.Identity.Commands.RevokeSession;
using SmartTaxi.Application.Identity.DataRequests.Commands.ProcessPersonalDataRequest;
using SmartTaxi.Application.Identity.DataRequests.Commands.SubmitPersonalDataRequest;
using SmartTaxi.Application.Identity.DataRequests.Queries.GetMyPersonalDataExportContent;
using SmartTaxi.Application.Identity.DataRequests.Queries.GetMyPersonalDataRequests;
using SmartTaxi.Application.Identity.DataRequests.Queries.GetPendingPersonalDataRequestsAdmin;
using SmartTaxi.Application.Identity.Preferences.Commands.UpdateMyPreferences;
using SmartTaxi.Application.Identity.Preferences.Queries.GetMyPreferences;
using SmartTaxi.Application.Identity.Professional.Commands.ApproveProfessionalAccountRequest;
using SmartTaxi.Application.Identity.Professional.Commands.ReactivateProfessionalAccountRequest;
using SmartTaxi.Application.Identity.Professional.Commands.RejectProfessionalAccountRequest;
using SmartTaxi.Application.Identity.Professional.Commands.SubmitProfessionalAccountRequest;
using SmartTaxi.Application.Identity.Professional.Commands.SuspendProfessionalAccountRequest;
using SmartTaxi.Application.Identity.Professional.Queries.GetMyProfessionalAccountRequests;
using SmartTaxi.Application.Identity.Professional.Queries.GetPendingProfessionalAccountRequests;
using SmartTaxi.Application.Identity.Professional.Queries.GetProfessionalAccountRequestByIdAdmin;
using SmartTaxi.Application.Identity.Queries.GetUserById;
using SmartTaxi.Application.Identity.Queries.GetUserSessions;
using SmartTaxi.Application.Identity.Referrals.Commands.EvaluateReferralActivation;
using SmartTaxi.Application.Identity.Referrals.Commands.InvalidateReferral;
using SmartTaxi.Application.Identity.Referrals.Commands.RegisterReferral;
using SmartTaxi.Application.Identity.Referrals.Queries.GetMyReferralCode;
using SmartTaxi.Application.Identity.Referrals.Queries.GetMyReferrals;
using SmartTaxi.Application.Identity.Referrals.Queries.GetPendingReferralsAdmin;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Application.Loyalty.Commands.ActivateChallenge;
using SmartTaxi.Application.Loyalty.Commands.ActivateEarningRule;
using SmartTaxi.Application.Loyalty.Commands.ActivateReward;
using SmartTaxi.Application.Loyalty.Commands.AdjustPoints;
using SmartTaxi.Application.Loyalty.Commands.CreateChallenge;
using SmartTaxi.Application.Loyalty.Commands.CreateEarningRule;
using SmartTaxi.Application.Loyalty.Commands.CreateReward;
using SmartTaxi.Application.Loyalty.Commands.DeactivateChallenge;
using SmartTaxi.Application.Loyalty.Commands.DeactivateEarningRule;
using SmartTaxi.Application.Loyalty.Commands.DeactivateReward;
using SmartTaxi.Application.Loyalty.Commands.ProcessChallengeProgressForUser;
using SmartTaxi.Application.Loyalty.Commands.ProcessExpiredPoints;
using SmartTaxi.Application.Loyalty.Commands.ProcessPaymentLoyaltyAward;
using SmartTaxi.Application.Loyalty.Commands.ProcessReferralRewards;
using SmartTaxi.Application.Loyalty.Commands.RedeemReward;
using SmartTaxi.Application.Loyalty.Commands.UpdateEarningRule;
using SmartTaxi.Application.Loyalty.Commands.UpdateReward;
using SmartTaxi.Application.Loyalty.Commands.UpdateTierThreshold;
using SmartTaxi.Application.Loyalty.Queries.GetActiveChallenges;
using SmartTaxi.Application.Loyalty.Queries.GetChallengesAdmin;
using SmartTaxi.Application.Loyalty.Queries.GetEarningRulesAdmin;
using SmartTaxi.Application.Loyalty.Queries.GetMyChallengeProgress;
using SmartTaxi.Application.Loyalty.Queries.GetMyLoyaltySummary;
using SmartTaxi.Application.Loyalty.Queries.GetMyPointLedger;
using SmartTaxi.Application.Loyalty.Queries.GetMyReferralRewardStatus;
using SmartTaxi.Application.Loyalty.Queries.GetRewardCatalog;
using SmartTaxi.Application.Loyalty.Queries.GetRewardCatalogAdmin;
using SmartTaxi.Application.Loyalty.Queries.GetTierThresholdsAdmin;
using SmartTaxi.Application.Loyalty;
using SmartTaxi.Application.Maintenance.Commands.CancelMaintenanceRequest;
using SmartTaxi.Application.Maintenance.Commands.CompleteMaintenance;
using SmartTaxi.Application.Maintenance.Commands.CreateMaintenanceRequest;
using SmartTaxi.Application.Maintenance.Commands.ForceCancelMaintenanceRequest;
using SmartTaxi.Application.Maintenance.Commands.GenerateMaintenanceReminders;
using SmartTaxi.Application.Maintenance.Commands.MarkVehicleReceived;
using SmartTaxi.Application.Maintenance.Commands.MarkWaitingForParts;
using SmartTaxi.Application.Maintenance.Commands.RegisterGarageProfile;
using SmartTaxi.Application.Maintenance.Commands.RespondToMaintenanceQuote;
using SmartTaxi.Application.Maintenance.Commands.RespondToMaintenanceRequest;
using SmartTaxi.Application.Maintenance.Commands.ResumeMaintenanceWork;
using SmartTaxi.Application.Maintenance.Commands.SettleMaintenanceRequest;
using SmartTaxi.Application.Maintenance.Commands.StartMaintenanceWork;
using SmartTaxi.Application.Maintenance.Commands.SubmitMaintenanceQuote;
using SmartTaxi.Application.Maintenance.Commands.UpdateGarageProfile;
using SmartTaxi.Application.Maintenance.Queries.GetAllMaintenanceRequests;
using SmartTaxi.Application.Maintenance.Queries.GetMaintenanceRequestDetails;
using SmartTaxi.Application.Maintenance.Queries.GetMyGarageJobs;
using SmartTaxi.Application.Maintenance.Queries.GetMyGarageProfile;
using SmartTaxi.Application.Maintenance.Queries.GetMyMaintenanceRequests;
using SmartTaxi.Application.Maintenance.Queries.GetMyVehicleMaintenanceHistory;
using SmartTaxi.Application.RoadsideAssistance.Commands.AcceptRoadsideJob;
using SmartTaxi.Application.RoadsideAssistance.Commands.CancelRoadsideAssistanceRequest;
using SmartTaxi.Application.RoadsideAssistance.Commands.CompleteRoadsideIntervention;
using SmartTaxi.Application.RoadsideAssistance.Commands.CreateRoadsideAssistanceRequest;
using SmartTaxi.Application.RoadsideAssistance.Commands.DisputeRoadsideAssistanceRequest;
using SmartTaxi.Application.RoadsideAssistance.Commands.EscalateRoadsideRequestToMaintenance;
using SmartTaxi.Application.RoadsideAssistance.Commands.ExpireStaleRoadsideRequests;
using SmartTaxi.Application.RoadsideAssistance.Commands.ForceCancelRoadsideRequest;
using SmartTaxi.Application.RoadsideAssistance.Commands.MarkPartnerArrived;
using SmartTaxi.Application.RoadsideAssistance.Commands.MarkPartnerOnTheWay;
using SmartTaxi.Application.RoadsideAssistance.Commands.RegisterRoadsidePartnerProfile;
using SmartTaxi.Application.RoadsideAssistance.Commands.RejectRoadsideJob;
using SmartTaxi.Application.RoadsideAssistance.Commands.ReselectRoadsideRequest;
using SmartTaxi.Application.RoadsideAssistance.Commands.SelectRoadsidePartner;
using SmartTaxi.Application.RoadsideAssistance.Commands.SettleRoadsideAssistanceRequest;
using SmartTaxi.Application.RoadsideAssistance.Commands.StartRoadsideIntervention;
using SmartTaxi.Application.RoadsideAssistance.Commands.UpdateRoadsidePartnerProfile;
using SmartTaxi.Application.RoadsideAssistance.Queries.GetAllRoadsideAssistanceRequests;
using SmartTaxi.Application.RoadsideAssistance.Queries.GetMyRoadsideAssistanceRequests;
using SmartTaxi.Application.RoadsideAssistance.Queries.GetMyRoadsideJobs;
using SmartTaxi.Application.RoadsideAssistance.Queries.GetMyRoadsidePartnerProfile;
using SmartTaxi.Application.RoadsideAssistance.Queries.GetRecommendedRoadsidePartners;
using SmartTaxi.Application.RoadsideAssistance.Queries.GetRoadsideAssistanceRequestById;
using SmartTaxi.Application.Support.Commands.AcknowledgeSupportIncident;
using SmartTaxi.Application.Support.Commands.AddAdminTicketMessage;
using SmartTaxi.Application.Support.Commands.AddInternalNote;
using SmartTaxi.Application.Support.Commands.AddTicketMessage;
using SmartTaxi.Application.Support.Commands.AssignSupportTicket;
using SmartTaxi.Application.Support.Commands.CloseSupportIncident;
using SmartTaxi.Application.Support.Commands.CloseSupportTicket;
using SmartTaxi.Application.Support.Commands.CreateIncidentFromFinancialDispute;
using SmartTaxi.Application.Support.Commands.CreateIncidentFromMaintenance;
using SmartTaxi.Application.Support.Commands.CreateIncidentFromRide;
using SmartTaxi.Application.Support.Commands.CreateIncidentFromRoadside;
using SmartTaxi.Application.Support.Commands.CreateSupportTicket;
using SmartTaxi.Application.Support.Commands.EscalateTicketToIncident;
using SmartTaxi.Application.Support.Commands.InvestigateSupportIncident;
using SmartTaxi.Application.Support.Commands.MarkIncidentFalsePositive;
using SmartTaxi.Application.Support.Commands.MarkWaitingForCustomer;
using SmartTaxi.Application.Support.Commands.ReassignSupportIncident;
using SmartTaxi.Application.Support.Commands.ReassignSupportTicket;
using SmartTaxi.Application.Support.Commands.ReopenSupportIncident;
using SmartTaxi.Application.Support.Commands.ReopenSupportTicket;
using SmartTaxi.Application.Support.Commands.ReportSupportIncident;
using SmartTaxi.Application.Support.Commands.ResolveSupportIncident;
using SmartTaxi.Application.Support.Commands.ResolveSupportTicket;
using SmartTaxi.Application.Support.Commands.StartSupportTicket;
using SmartTaxi.Application.Support.Queries.GetAllSupportIncidents;
using SmartTaxi.Application.Support.Queries.GetAllSupportTickets;
using SmartTaxi.Application.Support.Queries.GetMySupportTickets;
using SmartTaxi.Application.Support.Queries.GetSupportIncidentById;
using SmartTaxi.Application.Support.Queries.GetSupportTicketDetails;
using SmartTaxi.Application.Support.Queries.GetSupportTicketDetailsAdmin;
using SmartTaxi.Application.Administration.Commands.ReactivateUser;
using SmartTaxi.Application.Administration.Commands.ResetUserTwoFactor;
using SmartTaxi.Application.Administration.Commands.RevokeUserSessions;
using SmartTaxi.Application.Administration.Commands.SuspendUser;
using SmartTaxi.Application.Administration.Queries.GetAuditLogForTarget;
using SmartTaxi.Application.Analytics.Commands.CreateScheduledReport;
using SmartTaxi.Application.Analytics.Commands.DeactivateScheduledReport;
using SmartTaxi.Application.Analytics.Commands.ExportAnalyticsReport;
using SmartTaxi.Application.Analytics.Commands.ProcessDueScheduledReports;
using SmartTaxi.Application.Analytics.Commands.UpdateScheduledReport;
using SmartTaxi.Application.Analytics.Queries.GetAdminDashboard;
using SmartTaxi.Application.Analytics.Queries.GetAdvertisingAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetFinancialAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetFleetAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetGrowthAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetMaintenanceAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetRideAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetRoadsideAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetScheduledReports;
using SmartTaxi.Application.Analytics.Queries.GetSubscriptionAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetSupportAnalytics;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Commands.ActivateNotificationTemplate;
using SmartTaxi.Application.Notifications.Commands.CreateNotificationTemplate;
using SmartTaxi.Application.Notifications.Commands.DeactivateNotificationTemplate;
using SmartTaxi.Application.Notifications.Commands.MarkAllNotificationsRead;
using SmartTaxi.Application.Notifications.Commands.MarkNotificationRead;
using SmartTaxi.Application.Notifications.Commands.ProcessDueNotifications;
using SmartTaxi.Application.Notifications.Commands.ProcessRetryableDeliveries;
using SmartTaxi.Application.Notifications.Commands.RegisterDeviceToken;
using SmartTaxi.Application.Notifications.Commands.RevokeDeviceToken;
using SmartTaxi.Application.Notifications.Commands.ScheduleNotification;
using SmartTaxi.Application.Notifications.Commands.UpdateNotificationTemplate;
using SmartTaxi.Application.Notifications.Queries.GetMyNotifications;
using SmartTaxi.Application.Notifications.Queries.GetMyUnreadNotificationCount;
using SmartTaxi.Application.Notifications.Queries.GetNotificationDeliveryFailures;
using SmartTaxi.Application.Notifications.Queries.GetNotificationTemplates;
using SmartTaxi.Application.Payments.Commands.AuthorizePayment;
using SmartTaxi.Application.Payments.Commands.CancelPayment;
using SmartTaxi.Application.Payments.Commands.ConfirmPayment;
using SmartTaxi.Application.Payments.Commands.CreateRidePayment;
using SmartTaxi.Application.Payments.Commands.FailPayment;
using SmartTaxi.Application.Payments.Commands.RefundPayment;
using SmartTaxi.Application.Payments.Queries.GetAdminPayments;
using SmartTaxi.Application.Payments.Queries.GetDriverRevenueReport;
using SmartTaxi.Application.Payments.Queries.GetInvoiceByPaymentId;
using SmartTaxi.Application.Payments.Queries.GetMyPaymentsForCustomer;
using SmartTaxi.Application.Payments.Queries.GetMyPaymentsForDriver;
using SmartTaxi.Application.Payments.Queries.GetOwnerRevenueReport;
using SmartTaxi.Application.Payments.Queries.GetPaymentById;
using SmartTaxi.Application.Payments.Queries.GetPaymentByIdAdmin;
using SmartTaxi.Application.Payments.Queries.GetPaymentsForOwner;
using SmartTaxi.Application.Payments.Queries.GetPaymentStatistics;
using SmartTaxi.Application.Payments.Queries.GetPaymentTransactionHistory;
using SmartTaxi.Application.Payments.Queries.GetReceiptByPaymentId;
using SmartTaxi.Application.Payments.Queries.GetRefundsForPayment;
using SmartTaxi.Application.Payments.Queries.GetRevenueSummary;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Application.Rides.Commands.AcceptFare;
using SmartTaxi.Application.Rides.Commands.AcknowledgeSos;
using SmartTaxi.Application.Rides.Commands.ActivateSos;
using SmartTaxi.Application.Rides.Commands.ApproveSharedRideByDriver;
using SmartTaxi.Application.Rides.Commands.CancelRideByAdmin;
using SmartTaxi.Application.Rides.Commands.CancelRideByCustomer;
using SmartTaxi.Application.Rides.Commands.CancelRideByDriver;
using SmartTaxi.Application.Rides.Commands.CompleteRide;
using SmartTaxi.Application.Rides.Commands.CounterProposeFare;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Commands.CreateRideShareToken;
using SmartTaxi.Application.Rides.Commands.CustomerApproveSharedRide;
using SmartTaxi.Application.Rides.Commands.CustomerRejectSharedRide;
using SmartTaxi.Application.Rides.Commands.DismissRideComplaint;
using SmartTaxi.Application.Rides.Commands.DriverAcceptRide;
using SmartTaxi.Application.Rides.Commands.DriverArrived;
using SmartTaxi.Application.Rides.Commands.DriverEnRoute;
using SmartTaxi.Application.Rides.Commands.DriverRejectRide;
using SmartTaxi.Application.Rides.Commands.FindSharedRideMatch;
using SmartTaxi.Application.Rides.Commands.PassengerOnBoard;
using SmartTaxi.Application.Rides.Commands.ProposeFare;
using SmartTaxi.Application.Rides.Commands.RejectFare;
using SmartTaxi.Application.Rides.Commands.RejectSharedRideByDriver;
using SmartTaxi.Application.Rides.Commands.ReportCustomerNoShow;
using SmartTaxi.Application.Rides.Commands.ReportDriverNoShow;
using SmartTaxi.Application.Rides.Commands.ReportRideMessage;
using SmartTaxi.Application.Rides.Commands.ResolveRideComplaint;
using SmartTaxi.Application.Rides.Commands.ResolveSos;
using SmartTaxi.Application.Rides.Commands.RevokeRideShareToken;
using SmartTaxi.Application.Rides.Commands.SearchDrivers;
using SmartTaxi.Application.Rides.Commands.SelectDriver;
using SmartTaxi.Application.Rides.Commands.SendRideMessage;
using SmartTaxi.Application.Rides.Commands.StartRide;
using SmartTaxi.Application.Rides.Commands.SubmitRideComplaint;
using SmartTaxi.Application.Rides.Commands.SubmitRideRating;
using SmartTaxi.Application.Rides.Commands.UpdateDriverAvailabilityLocation;
using SmartTaxi.Application.Rides.Commands.UpdateDriverLocation;
using SmartTaxi.Application.Rides.Queries.GetActiveRidesAdmin;
using SmartTaxi.Application.Rides.Queries.GetAdminRides;
using SmartTaxi.Application.Rides.Queries.GetMyRidesForCustomer;
using SmartTaxi.Application.Rides.Queries.GetMyRidesForDriver;
using SmartTaxi.Application.Subscriptions.Commands.ActivateSubscriptionPlan;
using SmartTaxi.Application.Subscriptions.Commands.CancelSubscription;
using SmartTaxi.Application.Subscriptions.Commands.ChangeSubscriptionPlan;
using SmartTaxi.Application.Subscriptions.Commands.CreateSubscriptionPlan;
using SmartTaxi.Application.Subscriptions.Commands.DeactivateSubscriptionPlan;
using SmartTaxi.Application.Subscriptions.Commands.ExpireDueSubscriptions;
using SmartTaxi.Application.Subscriptions.Commands.RenewSubscription;
using SmartTaxi.Application.Subscriptions.Commands.Subscribe;
using SmartTaxi.Application.Subscriptions.Commands.UpdateSubscriptionPlan;
using SmartTaxi.Application.Subscriptions.Queries.GetAllSubscriptionPlans;
using SmartTaxi.Application.Subscriptions.Queries.GetAvailableSubscriptionPlans;
using SmartTaxi.Application.Subscriptions.Queries.GetMySubscription;
using SmartTaxi.Application.Subscriptions.Queries.GetSubscriptionHistory;
using SmartTaxi.Application.Subscriptions.Queries.GetSubscriptionPlanById;
using SmartTaxi.Application.Rides.Queries.GetPendingRideRequestsForDriver;
using SmartTaxi.Application.Rides.Queries.GetPublicRideShareView;
using SmartTaxi.Application.Rides.Queries.GetRecommendedDrivers;
using SmartTaxi.Application.Rides.Queries.GetRideById;
using SmartTaxi.Application.Rides.Queries.GetRideByIdAdmin;
using SmartTaxi.Application.Rides.Queries.GetRideComplaints;
using SmartTaxi.Application.Rides.Queries.GetRideMessages;
using SmartTaxi.Application.Rides.Queries.GetRideRatings;
using SmartTaxi.Application.Rides.Queries.GetRideSafetyEvents;
using SmartTaxi.Application.Rides.Queries.GetSharedRideFareBreakdown;
using SmartTaxi.Infrastructure;
using SmartTaxi.Infrastructure.Identity.Options;
using TwoFactorChallengeCommandNs = SmartTaxi.Application.Identity.Commands.TwoFactorChallenge;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers(); // SECURITY DEMO ONLY — required to host VulnerableSqlController; remove after CodeQL demo
builder.Services.AddSignalR();

// Used to encrypt TOTP secrets at rest (ITwoFactorSecretProtector). Registered
// here (not in Infrastructure) since key storage/isolation is a host concern.
builder.Services.AddDataProtection();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<RegisterUserCommandHandler>();
builder.Services.AddScoped<LoginUserCommandHandler>();
builder.Services.AddScoped<GetUserByIdQueryHandler>();
builder.Services.AddScoped<AssignRoleCommandHandler>();
builder.Services.AddScoped<RemoveRoleCommandHandler>();
builder.Services.AddScoped<SuspendUserCommandHandler>();
builder.Services.AddScoped<ReactivateUserCommandHandler>();
builder.Services.AddScoped<RevokeUserSessionsCommandHandler>();
builder.Services.AddScoped<ResetUserTwoFactorCommandHandler>();
builder.Services.AddScoped<GetAuditLogForTargetQueryHandler>();
builder.Services.AddScoped<RefreshTokenIssuer>();
builder.Services.AddScoped<RefreshTokenCommandHandler>();
builder.Services.AddScoped<LogoutCommandHandler>();
builder.Services.AddScoped<RevokeSessionCommandHandler>();
builder.Services.AddScoped<RevokeAllSessionsCommandHandler>();
builder.Services.AddScoped<GetUserSessionsQueryHandler>();
builder.Services.AddScoped<RequestEmailVerificationCommandHandler>();
builder.Services.AddScoped<ConfirmEmailVerificationCommandHandler>();
builder.Services.AddScoped<RequestPhoneVerificationCommandHandler>();
builder.Services.AddScoped<ConfirmPhoneVerificationCommandHandler>();
builder.Services.AddScoped<ForgotPasswordCommandHandler>();
builder.Services.AddScoped<ResetPasswordCommandHandler>();
builder.Services.AddScoped<ChangePasswordCommandHandler>();
builder.Services.AddScoped<EnrollTwoFactorCommandHandler>();
builder.Services.AddScoped<ConfirmTwoFactorCommandHandler>();
builder.Services.AddScoped<TwoFactorChallengeCommandNs.TwoFactorChallengeCommandHandler>();
builder.Services.AddScoped<DisableTwoFactorCommandHandler>();
builder.Services.AddScoped<RegenerateRecoveryCodesCommandHandler>();
builder.Services.AddScoped<UploadDocumentCommandHandler>();
builder.Services.AddScoped<ReplaceDocumentCommandHandler>();
builder.Services.AddScoped<CancelDocumentCommandHandler>();
builder.Services.AddScoped<ApproveDocumentCommandHandler>();
builder.Services.AddScoped<RejectDocumentCommandHandler>();
builder.Services.AddScoped<SuspendDocumentCommandHandler>();
builder.Services.AddScoped<ExpireDocumentsCommandHandler>();
builder.Services.AddScoped<GetMyDocumentsQueryHandler>();
builder.Services.AddScoped<GetMyDocumentByIdQueryHandler>();
builder.Services.AddScoped<GetPendingDocumentsQueryHandler>();
builder.Services.AddScoped<GetDocumentByIdAdminQueryHandler>();
builder.Services.AddScoped<GetMyDocumentContentQueryHandler>();
builder.Services.AddScoped<GetDocumentContentAdminQueryHandler>();
builder.Services.AddScoped<GetUserProfessionalEligibilityQueryHandler>();
builder.Services.AddScoped<SubmitProfessionalAccountRequestCommandHandler>();
builder.Services.AddScoped<ApproveProfessionalAccountRequestCommandHandler>();
builder.Services.AddScoped<RejectProfessionalAccountRequestCommandHandler>();
builder.Services.AddScoped<SuspendProfessionalAccountRequestCommandHandler>();
builder.Services.AddScoped<ReactivateProfessionalAccountRequestCommandHandler>();
builder.Services.AddScoped<GetMyProfessionalAccountRequestsQueryHandler>();
builder.Services.AddScoped<GetPendingProfessionalAccountRequestsQueryHandler>();
builder.Services.AddScoped<GetProfessionalAccountRequestByIdAdminQueryHandler>();
builder.Services.AddScoped<RegisterReferralCommandHandler>();
builder.Services.AddScoped<EvaluateReferralActivationCommandHandler>();
builder.Services.AddScoped<InvalidateReferralCommandHandler>();
builder.Services.AddScoped<GetMyReferralCodeQueryHandler>();
builder.Services.AddScoped<GetMyReferralsQueryHandler>();
builder.Services.AddScoped<GetPendingReferralsAdminQueryHandler>();
builder.Services.AddScoped<UpdateMyPreferencesCommandHandler>();
builder.Services.AddScoped<GetMyPreferencesQueryHandler>();
builder.Services.AddScoped<SubmitPersonalDataRequestCommandHandler>();
builder.Services.AddScoped<ProcessPersonalDataRequestCommandHandler>();
builder.Services.AddScoped<GetMyPersonalDataRequestsQueryHandler>();
builder.Services.AddScoped<GetPendingPersonalDataRequestsAdminQueryHandler>();
builder.Services.AddScoped<GetMyPersonalDataExportContentQueryHandler>();

// Fleet module.
builder.Services.AddScoped<CreateIndividualOwnerProfileCommandHandler>();
builder.Services.AddScoped<CreateCompanyOwnerProfileCommandHandler>();
builder.Services.AddScoped<UpdateOwnerProfileCommandHandler>();
builder.Services.AddScoped<GetMyOwnerProfileQueryHandler>();

builder.Services.AddScoped<CreateFleetCommandHandler>();
builder.Services.AddScoped<UpdateFleetCommandHandler>();
builder.Services.AddScoped<SuspendFleetCommandHandler>();
builder.Services.AddScoped<AddFleetCollaboratorCommandHandler>();
builder.Services.AddScoped<RemoveFleetCollaboratorCommandHandler>();
builder.Services.AddScoped<GetFleetByIdQueryHandler>();
builder.Services.AddScoped<GetMyFleetsQueryHandler>();
builder.Services.AddScoped<GetFleetCollaboratorsQueryHandler>();

builder.Services.AddScoped<RegisterVehicleCommandHandler>();
builder.Services.AddScoped<UpdateVehicleCommandHandler>();
builder.Services.AddScoped<SubmitVehicleForVerificationCommandHandler>();
builder.Services.AddScoped<ApproveVehicleCommandHandler>();
builder.Services.AddScoped<RejectVehicleCommandHandler>();
builder.Services.AddScoped<SuspendVehicleCommandHandler>();
builder.Services.AddScoped<RetireVehicleCommandHandler>();
builder.Services.AddScoped<GetVehicleByIdQueryHandler>();
builder.Services.AddScoped<GetOwnerVehiclesQueryHandler>();
builder.Services.AddScoped<GetFleetVehiclesQueryHandler>();
builder.Services.AddScoped<GetVehicleEligibilityQueryHandler>();

builder.Services.AddScoped<UploadVehicleDocumentCommandHandler>();
builder.Services.AddScoped<ReplaceVehicleDocumentCommandHandler>();
builder.Services.AddScoped<ApproveVehicleDocumentCommandHandler>();
builder.Services.AddScoped<RejectVehicleDocumentCommandHandler>();
builder.Services.AddScoped<SuspendVehicleDocumentCommandHandler>();
builder.Services.AddScoped<ExpireVehicleDocumentsCommandHandler>();
builder.Services.AddScoped<GetVehicleDocumentsQueryHandler>();
builder.Services.AddScoped<GetPendingVehicleDocumentsQueryHandler>();
builder.Services.AddScoped<GetVehicleDocumentContentQueryHandler>();
builder.Services.AddScoped<GetVehicleDocumentContentAdminQueryHandler>();

builder.Services.AddScoped<CreateDriverProfileCommandHandler>();
builder.Services.AddScoped<UpdateDriverProfileCommandHandler>();
builder.Services.AddScoped<SubmitDriverForReviewCommandHandler>();
builder.Services.AddScoped<ApproveDriverCommandHandler>();
builder.Services.AddScoped<RejectDriverCommandHandler>();
builder.Services.AddScoped<SuspendDriverCommandHandler>();
builder.Services.AddScoped<SetDriverAvailabilityCommandHandler>();
builder.Services.AddScoped<GetDriverProfileByIdQueryHandler>();
builder.Services.AddScoped<GetMyDriverProfileQueryHandler>();
builder.Services.AddScoped<GetEligibleDriversQueryHandler>();

builder.Services.AddScoped<CreateAssignmentCommandHandler>();
builder.Services.AddScoped<ApproveAssignmentCommandHandler>();
builder.Services.AddScoped<ActivateAssignmentCommandHandler>();
builder.Services.AddScoped<SuspendAssignmentCommandHandler>();
builder.Services.AddScoped<CompleteAssignmentCommandHandler>();
builder.Services.AddScoped<CancelAssignmentCommandHandler>();
builder.Services.AddScoped<GetDriverAssignmentsQueryHandler>();
builder.Services.AddScoped<GetVehicleAssignmentsQueryHandler>();
builder.Services.AddScoped<ValidateAssignmentAvailabilityQueryHandler>();

builder.Services.AddScoped<CreateContractCommandHandler>();
builder.Services.AddScoped<UpdateDraftContractCommandHandler>();
builder.Services.AddScoped<SubmitContractCommandHandler>();
builder.Services.AddScoped<ActivateContractCommandHandler>();
builder.Services.AddScoped<SuspendContractCommandHandler>();
builder.Services.AddScoped<TerminateContractCommandHandler>();
builder.Services.AddScoped<GetContractsForOwnerQueryHandler>();
builder.Services.AddScoped<GetContractsForDriverQueryHandler>();
builder.Services.AddScoped<GetActiveContractQueryHandler>();

builder.Services.AddScoped<CreateExpenseCommandHandler>();
builder.Services.AddScoped<SubmitExpenseCommandHandler>();
builder.Services.AddScoped<ApproveExpenseCommandHandler>();
builder.Services.AddScoped<RejectExpenseCommandHandler>();
builder.Services.AddScoped<MarkExpensePaidCommandHandler>();
builder.Services.AddScoped<ListExpensesQueryHandler>();

builder.Services.AddScoped<GetVehicleUsageHistoryQueryHandler>();
builder.Services.AddScoped<GetDriverUsageHistoryQueryHandler>();

builder.Services.AddScoped<GenerateFleetAlertsCommandHandler>();
builder.Services.AddScoped<ResolveFleetAlertCommandHandler>();
builder.Services.AddScoped<DismissFleetAlertCommandHandler>();
builder.Services.AddScoped<GetFleetAlertsQueryHandler>();

// Ride module.
builder.Services.AddScoped<CreateRideCommandHandler>();
builder.Services.AddScoped<SearchDriversCommandHandler>();
builder.Services.AddScoped<GetRecommendedDriversQueryHandler>();
builder.Services.AddScoped<SelectDriverCommandHandler>();
builder.Services.AddScoped<DriverAcceptRideCommandHandler>();
builder.Services.AddScoped<DriverRejectRideCommandHandler>();
builder.Services.AddScoped<DriverEnRouteCommandHandler>();
builder.Services.AddScoped<DriverArrivedCommandHandler>();
builder.Services.AddScoped<PassengerOnBoardCommandHandler>();
builder.Services.AddScoped<StartRideCommandHandler>();
builder.Services.AddScoped<CompleteRideCommandHandler>();
builder.Services.AddScoped<CancelRideByCustomerCommandHandler>();
builder.Services.AddScoped<CancelRideByDriverCommandHandler>();
builder.Services.AddScoped<CancelRideByAdminCommandHandler>();
builder.Services.AddScoped<ReportCustomerNoShowCommandHandler>();
builder.Services.AddScoped<ReportDriverNoShowCommandHandler>();
builder.Services.AddScoped<UpdateDriverLocationCommandHandler>();
builder.Services.AddScoped<UpdateDriverAvailabilityLocationCommandHandler>();
builder.Services.AddScoped<GetRideByIdQueryHandler>();
builder.Services.AddScoped<GetMyRidesForCustomerQueryHandler>();
builder.Services.AddScoped<GetMyRidesForDriverQueryHandler>();
builder.Services.AddScoped<GetPendingRideRequestsForDriverQueryHandler>();

builder.Services.AddScoped<ProposeFareCommandHandler>();
builder.Services.AddScoped<CounterProposeFareCommandHandler>();
builder.Services.AddScoped<AcceptFareCommandHandler>();
builder.Services.AddScoped<RejectFareCommandHandler>();

builder.Services.AddScoped<FindSharedRideMatchCommandHandler>();
builder.Services.AddScoped<CustomerApproveSharedRideCommandHandler>();
builder.Services.AddScoped<CustomerRejectSharedRideCommandHandler>();
builder.Services.AddScoped<ApproveSharedRideByDriverCommandHandler>();
builder.Services.AddScoped<RejectSharedRideByDriverCommandHandler>();
builder.Services.AddScoped<GetSharedRideFareBreakdownQueryHandler>();

builder.Services.AddScoped<SubmitRideRatingCommandHandler>();
builder.Services.AddScoped<SubmitRideComplaintCommandHandler>();
builder.Services.AddScoped<ResolveRideComplaintCommandHandler>();
builder.Services.AddScoped<DismissRideComplaintCommandHandler>();
builder.Services.AddScoped<ActivateSosCommandHandler>();
builder.Services.AddScoped<AcknowledgeSosCommandHandler>();
builder.Services.AddScoped<ResolveSosCommandHandler>();
builder.Services.AddScoped<CreateRideShareTokenCommandHandler>();
builder.Services.AddScoped<RevokeRideShareTokenCommandHandler>();
builder.Services.AddScoped<GetPublicRideShareViewQueryHandler>();
builder.Services.AddScoped<SendRideMessageCommandHandler>();
builder.Services.AddScoped<GetRideMessagesQueryHandler>();
builder.Services.AddScoped<ReportRideMessageCommandHandler>();

builder.Services.AddScoped<GetAdminRidesQueryHandler>();
builder.Services.AddScoped<GetActiveRidesAdminQueryHandler>();
builder.Services.AddScoped<GetRideByIdAdminQueryHandler>();
builder.Services.AddScoped<GetRideComplaintsQueryHandler>();
builder.Services.AddScoped<GetRideSafetyEventsQueryHandler>();
builder.Services.AddScoped<GetRideRatingsQueryHandler>();

// Real SignalR-backed implementation lives here (API layer), not Infrastructure,
// so Application never references Microsoft.AspNetCore.SignalR.
builder.Services.AddScoped<IRideRealtimeNotifier, SignalRRideRealtimeNotifier>();
builder.Services.AddScoped<INotificationRealtimeNotifier, SignalRNotificationRealtimeNotifier>();

// Payments module.
builder.Services.AddScoped<CreateRidePaymentCommandHandler>();
builder.Services.AddScoped<AuthorizePaymentCommandHandler>();
builder.Services.AddScoped<ConfirmPaymentCommandHandler>();
builder.Services.AddScoped<CancelPaymentCommandHandler>();
builder.Services.AddScoped<FailPaymentCommandHandler>();
builder.Services.AddScoped<RefundPaymentCommandHandler>();
builder.Services.AddScoped<GetPaymentByIdQueryHandler>();
builder.Services.AddScoped<GetPaymentByIdAdminQueryHandler>();
builder.Services.AddScoped<GetMyPaymentsForCustomerQueryHandler>();
builder.Services.AddScoped<GetMyPaymentsForDriverQueryHandler>();
builder.Services.AddScoped<GetPaymentsForOwnerQueryHandler>();
builder.Services.AddScoped<GetAdminPaymentsQueryHandler>();
builder.Services.AddScoped<GetPaymentTransactionHistoryQueryHandler>();
builder.Services.AddScoped<GetInvoiceByPaymentIdQueryHandler>();
builder.Services.AddScoped<GetReceiptByPaymentIdQueryHandler>();
builder.Services.AddScoped<GetRefundsForPaymentQueryHandler>();
builder.Services.AddScoped<GetRevenueSummaryQueryHandler>();
builder.Services.AddScoped<GetDriverRevenueReportQueryHandler>();
builder.Services.AddScoped<GetOwnerRevenueReportQueryHandler>();
builder.Services.AddScoped<GetPaymentStatisticsQueryHandler>();

// Subscription module.
builder.Services.AddScoped<CreateSubscriptionPlanCommandHandler>();
builder.Services.AddScoped<UpdateSubscriptionPlanCommandHandler>();
builder.Services.AddScoped<ActivateSubscriptionPlanCommandHandler>();
builder.Services.AddScoped<DeactivateSubscriptionPlanCommandHandler>();
builder.Services.AddScoped<GetAllSubscriptionPlansQueryHandler>();
builder.Services.AddScoped<GetAvailableSubscriptionPlansQueryHandler>();
builder.Services.AddScoped<GetSubscriptionPlanByIdQueryHandler>();
builder.Services.AddScoped<SubscribeCommandHandler>();
builder.Services.AddScoped<RenewSubscriptionCommandHandler>();
builder.Services.AddScoped<CancelSubscriptionCommandHandler>();
builder.Services.AddScoped<ChangeSubscriptionPlanCommandHandler>();
builder.Services.AddScoped<ExpireDueSubscriptionsCommandHandler>();
builder.Services.AddScoped<GetMySubscriptionQueryHandler>();
builder.Services.AddScoped<GetSubscriptionHistoryQueryHandler>();

builder.Services.AddScoped<MarkNotificationReadCommandHandler>();
builder.Services.AddScoped<MarkAllNotificationsReadCommandHandler>();
builder.Services.AddScoped<RegisterDeviceTokenCommandHandler>();
builder.Services.AddScoped<RevokeDeviceTokenCommandHandler>();
builder.Services.AddScoped<CreateNotificationTemplateCommandHandler>();
builder.Services.AddScoped<UpdateNotificationTemplateCommandHandler>();
builder.Services.AddScoped<ActivateNotificationTemplateCommandHandler>();
builder.Services.AddScoped<DeactivateNotificationTemplateCommandHandler>();
builder.Services.AddScoped<ScheduleNotificationCommandHandler>();
builder.Services.AddScoped<ProcessDueNotificationsCommandHandler>();
builder.Services.AddScoped<ProcessRetryableDeliveriesCommandHandler>();
builder.Services.AddScoped<GetMyNotificationsQueryHandler>();
builder.Services.AddScoped<GetMyUnreadNotificationCountQueryHandler>();
builder.Services.AddScoped<GetNotificationTemplatesQueryHandler>();
builder.Services.AddScoped<GetNotificationDeliveryFailuresQueryHandler>();

builder.Services.AddScoped<LoyaltyReferralRewardGranter>();
builder.Services.AddScoped<ProcessChallengeProgressForUserCommandHandler>();
builder.Services.AddScoped<ProcessPaymentLoyaltyAwardCommandHandler>();
builder.Services.AddScoped<ProcessReferralRewardsCommandHandler>();
builder.Services.AddScoped<ProcessExpiredPointsCommandHandler>();
builder.Services.AddScoped<RedeemRewardCommandHandler>();
builder.Services.AddScoped<CreateEarningRuleCommandHandler>();
builder.Services.AddScoped<UpdateEarningRuleCommandHandler>();
builder.Services.AddScoped<ActivateEarningRuleCommandHandler>();
builder.Services.AddScoped<DeactivateEarningRuleCommandHandler>();
builder.Services.AddScoped<UpdateTierThresholdCommandHandler>();
builder.Services.AddScoped<CreateRewardCommandHandler>();
builder.Services.AddScoped<UpdateRewardCommandHandler>();
builder.Services.AddScoped<ActivateRewardCommandHandler>();
builder.Services.AddScoped<DeactivateRewardCommandHandler>();
builder.Services.AddScoped<CreateChallengeCommandHandler>();
builder.Services.AddScoped<ActivateChallengeCommandHandler>();
builder.Services.AddScoped<DeactivateChallengeCommandHandler>();
builder.Services.AddScoped<AdjustPointsCommandHandler>();
builder.Services.AddScoped<GetMyLoyaltySummaryQueryHandler>();
builder.Services.AddScoped<GetMyPointLedgerQueryHandler>();
builder.Services.AddScoped<GetRewardCatalogQueryHandler>();
builder.Services.AddScoped<GetMyReferralRewardStatusQueryHandler>();
builder.Services.AddScoped<GetActiveChallengesQueryHandler>();
builder.Services.AddScoped<GetMyChallengeProgressQueryHandler>();
builder.Services.AddScoped<GetEarningRulesAdminQueryHandler>();
builder.Services.AddScoped<GetTierThresholdsAdminQueryHandler>();
builder.Services.AddScoped<GetRewardCatalogAdminQueryHandler>();
builder.Services.AddScoped<GetChallengesAdminQueryHandler>();

builder.Services.AddScoped<RegisterAdvertiserProfileCommandHandler>();
builder.Services.AddScoped<UpdateAdvertiserProfileCommandHandler>();
builder.Services.AddScoped<CreateCampaignCommandHandler>();
builder.Services.AddScoped<UpdateCampaignCommandHandler>();
builder.Services.AddScoped<SubmitCampaignCommandHandler>();
builder.Services.AddScoped<CancelCampaignCommandHandler>();
builder.Services.AddScoped<PauseCampaignCommandHandler>();
builder.Services.AddScoped<ResumeCampaignCommandHandler>();
builder.Services.AddScoped<ApproveCampaignCommandHandler>();
builder.Services.AddScoped<RejectCampaignCommandHandler>();
builder.Services.AddScoped<RequestCampaignChangesCommandHandler>();
builder.Services.AddScoped<SuspendCampaignCommandHandler>();
builder.Services.AddScoped<ReactivateCampaignCommandHandler>();
builder.Services.AddScoped<SettleCampaignBudgetCommandHandler>();
builder.Services.AddScoped<UploadCampaignMediaCommandHandler>();
builder.Services.AddScoped<RequestAdDeliveryCommandHandler>();
builder.Services.AddScoped<RecordImpressionCommandHandler>();
builder.Services.AddScoped<RecordClickCommandHandler>();
builder.Services.AddScoped<ProcessScheduledCampaignsCommandHandler>();
builder.Services.AddScoped<ProcessCompletedCampaignsCommandHandler>();
builder.Services.AddScoped<CreatePlacementCommandHandler>();
builder.Services.AddScoped<UpdatePlacementCommandHandler>();
builder.Services.AddScoped<ActivatePlacementCommandHandler>();
builder.Services.AddScoped<DeactivatePlacementCommandHandler>();
builder.Services.AddScoped<GetMyAdvertiserProfileQueryHandler>();
builder.Services.AddScoped<GetMyCampaignsQueryHandler>();
builder.Services.AddScoped<GetCampaignDetailsQueryHandler>();
builder.Services.AddScoped<GetCampaignDetailsAdminQueryHandler>();
builder.Services.AddScoped<GetCampaignPerformanceQueryHandler>();
builder.Services.AddScoped<GetCampaignPerformanceAdminQueryHandler>();
builder.Services.AddScoped<GetCampaignMediaContentQueryHandler>();
builder.Services.AddScoped<GetPendingReviewCampaignsQueryHandler>();
builder.Services.AddScoped<GetPlacementsQueryHandler>();
builder.Services.AddScoped<GetPlacementsAdminQueryHandler>();

builder.Services.AddScoped<RegisterGarageProfileCommandHandler>();
builder.Services.AddScoped<UpdateGarageProfileCommandHandler>();
builder.Services.AddScoped<CreateMaintenanceRequestCommandHandler>();
builder.Services.AddScoped<RespondToMaintenanceRequestCommandHandler>();
builder.Services.AddScoped<CancelMaintenanceRequestCommandHandler>();
builder.Services.AddScoped<SubmitMaintenanceQuoteCommandHandler>();
builder.Services.AddScoped<RespondToMaintenanceQuoteCommandHandler>();
builder.Services.AddScoped<MarkVehicleReceivedCommandHandler>();
builder.Services.AddScoped<StartMaintenanceWorkCommandHandler>();
builder.Services.AddScoped<MarkWaitingForPartsCommandHandler>();
builder.Services.AddScoped<ResumeMaintenanceWorkCommandHandler>();
builder.Services.AddScoped<CompleteMaintenanceCommandHandler>();
builder.Services.AddScoped<SettleMaintenanceRequestCommandHandler>();
builder.Services.AddScoped<ForceCancelMaintenanceRequestCommandHandler>();
builder.Services.AddScoped<GenerateMaintenanceRemindersCommandHandler>();
builder.Services.AddScoped<GetMyGarageProfileQueryHandler>();
builder.Services.AddScoped<GetMyMaintenanceRequestsQueryHandler>();
builder.Services.AddScoped<GetMaintenanceRequestDetailsQueryHandler>();
builder.Services.AddScoped<GetMyGarageJobsQueryHandler>();
builder.Services.AddScoped<GetMyVehicleMaintenanceHistoryQueryHandler>();
builder.Services.AddScoped<GetAllMaintenanceRequestsQueryHandler>();

builder.Services.AddScoped<RegisterRoadsidePartnerProfileCommandHandler>();
builder.Services.AddScoped<UpdateRoadsidePartnerProfileCommandHandler>();
builder.Services.AddScoped<CreateRoadsideAssistanceRequestCommandHandler>();
builder.Services.AddScoped<SelectRoadsidePartnerCommandHandler>();
builder.Services.AddScoped<ReselectRoadsideRequestCommandHandler>();
builder.Services.AddScoped<AcceptRoadsideJobCommandHandler>();
builder.Services.AddScoped<RejectRoadsideJobCommandHandler>();
builder.Services.AddScoped<MarkPartnerOnTheWayCommandHandler>();
builder.Services.AddScoped<MarkPartnerArrivedCommandHandler>();
builder.Services.AddScoped<StartRoadsideInterventionCommandHandler>();
builder.Services.AddScoped<CompleteRoadsideInterventionCommandHandler>();
builder.Services.AddScoped<CancelRoadsideAssistanceRequestCommandHandler>();
builder.Services.AddScoped<ForceCancelRoadsideRequestCommandHandler>();
builder.Services.AddScoped<SettleRoadsideAssistanceRequestCommandHandler>();
builder.Services.AddScoped<DisputeRoadsideAssistanceRequestCommandHandler>();
builder.Services.AddScoped<ExpireStaleRoadsideRequestsCommandHandler>();
builder.Services.AddScoped<EscalateRoadsideRequestToMaintenanceCommandHandler>();
builder.Services.AddScoped<GetMyRoadsidePartnerProfileQueryHandler>();
builder.Services.AddScoped<GetMyRoadsideAssistanceRequestsQueryHandler>();
builder.Services.AddScoped<GetRoadsideAssistanceRequestByIdQueryHandler>();
builder.Services.AddScoped<GetMyRoadsideJobsQueryHandler>();
builder.Services.AddScoped<GetRecommendedRoadsidePartnersQueryHandler>();
builder.Services.AddScoped<GetAllRoadsideAssistanceRequestsQueryHandler>();

builder.Services.AddScoped<CreateSupportTicketCommandHandler>();
builder.Services.AddScoped<AddTicketMessageCommandHandler>();
builder.Services.AddScoped<AddAdminTicketMessageCommandHandler>();
builder.Services.AddScoped<AddInternalNoteCommandHandler>();
builder.Services.AddScoped<AssignSupportTicketCommandHandler>();
builder.Services.AddScoped<ReassignSupportTicketCommandHandler>();
builder.Services.AddScoped<StartSupportTicketCommandHandler>();
builder.Services.AddScoped<MarkWaitingForCustomerCommandHandler>();
builder.Services.AddScoped<ResolveSupportTicketCommandHandler>();
builder.Services.AddScoped<CloseSupportTicketCommandHandler>();
builder.Services.AddScoped<ReopenSupportTicketCommandHandler>();
builder.Services.AddScoped<EscalateTicketToIncidentCommandHandler>();
builder.Services.AddScoped<GetMySupportTicketsQueryHandler>();
builder.Services.AddScoped<GetAllSupportTicketsQueryHandler>();
builder.Services.AddScoped<GetSupportTicketDetailsQueryHandler>();
builder.Services.AddScoped<GetSupportTicketDetailsAdminQueryHandler>();

builder.Services.AddScoped<ReportSupportIncidentCommandHandler>();
builder.Services.AddScoped<CreateIncidentFromRideCommandHandler>();
builder.Services.AddScoped<CreateIncidentFromFinancialDisputeCommandHandler>();
builder.Services.AddScoped<CreateIncidentFromRoadsideCommandHandler>();
builder.Services.AddScoped<CreateIncidentFromMaintenanceCommandHandler>();
builder.Services.AddScoped<AcknowledgeSupportIncidentCommandHandler>();
builder.Services.AddScoped<ReassignSupportIncidentCommandHandler>();
builder.Services.AddScoped<InvestigateSupportIncidentCommandHandler>();
builder.Services.AddScoped<ResolveSupportIncidentCommandHandler>();
builder.Services.AddScoped<CloseSupportIncidentCommandHandler>();
builder.Services.AddScoped<ReopenSupportIncidentCommandHandler>();
builder.Services.AddScoped<MarkIncidentFalsePositiveCommandHandler>();
builder.Services.AddScoped<GetAllSupportIncidentsQueryHandler>();
builder.Services.AddScoped<GetSupportIncidentByIdQueryHandler>();

builder.Services.AddScoped<GetAdminDashboardQueryHandler>();
builder.Services.AddScoped<GetGrowthAnalyticsQueryHandler>();
builder.Services.AddScoped<GetRideAnalyticsQueryHandler>();
builder.Services.AddScoped<GetFleetAnalyticsQueryHandler>();
builder.Services.AddScoped<GetSubscriptionAnalyticsQueryHandler>();
builder.Services.AddScoped<GetAdvertisingAnalyticsQueryHandler>();
builder.Services.AddScoped<GetMaintenanceAnalyticsQueryHandler>();
builder.Services.AddScoped<GetRoadsideAnalyticsQueryHandler>();
builder.Services.AddScoped<GetSupportAnalyticsQueryHandler>();
builder.Services.AddScoped<GetFinancialAnalyticsQueryHandler>();
builder.Services.AddScoped<CreateScheduledReportCommandHandler>();
builder.Services.AddScoped<UpdateScheduledReportCommandHandler>();
builder.Services.AddScoped<DeactivateScheduledReportCommandHandler>();
builder.Services.AddScoped<ProcessDueScheduledReportsCommandHandler>();
builder.Services.AddScoped<GetScheduledReportsQueryHandler>();
builder.Services.AddScoped<ExportAnalyticsReportCommandHandler>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Single source of truth for JWT settings: the same JwtOptions type/section
// bound in SmartTaxi.Infrastructure.DependencyInjection.AddInfrastructure
// (used by JwtTokenGenerator), instead of re-reading raw configuration keys.
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException("La clé JWT ('Jwt:Key') n'est pas configurée. Définissez-la via dotnet user-secrets.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

// One claim-based policy per known permission code — new permissions added by
// future modules just need to be added to Permissions.All, no further changes here.
var authorizationBuilder = builder.Services.AddAuthorizationBuilder();
foreach (var permission in Permissions.All)
{
    authorizationBuilder.AddPolicy(permission, policy => policy.RequireClaim(Permissions.ClaimType, permission));
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapAdminUserManagementEndpoints();
app.MapAuditLogEndpoints();
app.MapDocumentEndpoints();
app.MapAdminDocumentEndpoints();
app.MapProfessionalAccountEndpoints();
app.MapReferralEndpoints();
app.MapPreferencesEndpoints();
app.MapDataRequestEndpoints();

app.MapOwnerEndpoints();
app.MapFleetEndpoints();
app.MapVehicleEndpoints();
app.MapVehicleDocumentEndpoints();
app.MapAdminVehicleDocumentEndpoints();
app.MapDriverEndpoints();
app.MapAssignmentEndpoints();
app.MapContractEndpoints();
app.MapExpenseEndpoints();
app.MapUsageHistoryEndpoints();
app.MapAlertEndpoints();

app.MapRideEndpoints();
app.MapRideNegotiationEndpoints();
app.MapRideSharedRideEndpoints();
app.MapRideFeedbackEndpoints();
app.MapRideAdminEndpoints();
app.MapHub<RideHub>("/hubs/rides");
app.MapHub<NotificationHub>("/hubs/notifications");

app.MapPaymentEndpoints();
app.MapPaymentAdminEndpoints();
app.MapPaymentReportEndpoints();
app.MapPayoutEndpoints();
app.MapPayoutAdminEndpoints();
app.MapCashRegisterEndpoints();
app.MapCashRegisterAdminEndpoints();
app.MapCashDeclarationEndpoints();
app.MapCashDeclarationAdminEndpoints();
app.MapBusinessCustomerEndpoints();
app.MapBusinessCustomerAdminEndpoints();
app.MapTaxRuleAdminEndpoints();
app.MapGroupedInvoiceEndpoints();
app.MapGroupedInvoiceAdminEndpoints();
app.MapFinancialDisputeEndpoints();
app.MapFinancialDisputeAdminEndpoints();
app.MapFinancialReportAdminEndpoints();

app.MapSubscriptionPlanEndpoints();
app.MapSubscriptionPlanAdminEndpoints();
app.MapSubscriptionEndpoints();
app.MapSubscriptionAdminEndpoints();

app.MapNotificationEndpoints();
app.MapNotificationAdminEndpoints();

app.MapLoyaltyEndpoints();
app.MapLoyaltyAdminEndpoints();
app.MapAdvertisingEndpoints();
app.MapAdvertisingAdminEndpoints();

app.MapMaintenanceEndpoints();
app.MapMaintenanceAdminEndpoints();

app.MapRoadsideAssistanceEndpoints();
app.MapRoadsideAssistanceAdminEndpoints();

app.MapSupportTicketEndpoints();
app.MapSupportTicketAdminEndpoints();
app.MapSupportIncidentAdminEndpoints();

app.MapAnalyticsEndpoints();
app.MapScheduledReportEndpoints();

app.MapControllers(); // SECURITY DEMO ONLY — hosts VulnerableSqlController; remove after CodeQL demo

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
