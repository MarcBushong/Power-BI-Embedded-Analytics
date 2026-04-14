import 'bootstrap';

// Use CommonJS-style import so $ resolves to the jQuery function, not a namespace object
import $ = require('jquery');

import * as powerbi from "powerbi-client";
import * as pbimodels from "powerbi-models";

require('powerbi-models');
require('powerbi-client');

import SpaAuthService from './services/SpaAuthService';
import AppOwnsDataWebApi from './services/AppOwnsDataWebApi'

import { Report, Dataset, ViewModel, TenantTheme, ActivityLogEntry } from './models/models';

export default class App {

  // fields for UI elemenets in DOM
  private static mainBody: JQuery;
  private static topBanner: JQuery;
  private static userGreeting: JQuery;
  private static login: JQuery;
  private static logout: JQuery;
  private static viewAnonymous: JQuery;
  private static viewUnassigned: JQuery;
  private static loadingSpinner: JQuery;
  private static loadingSpinnerMessage: JQuery;
  private static viewAuthenticated: JQuery;
  private static viewAuthenticatedHeader: JQuery;
  private static tenantName: JQuery;
  private static reportsList: JQuery;
  private static datasetsList: JQuery;
  private static datasetsListContainer: JQuery;
  private static embedToolbar: JQuery;
  private static breadcrumb: JQuery;
  private static toggleEditButton: JQuery;
  private static fullScreenButton: JQuery;
  private static embedContainer: JQuery;
  private static brandBanner: JQuery;
  private static resizedFinished: any;
  private static currentReport: powerbi.Report;
  private static layoutMode: "master" | "mobile";
  private static breakPointWidth: number = 576;

  private static powerbi: powerbi.service.Service = window.powerbi;
  private static viewModel: ViewModel;

  public static onDocumentReady = () => {

    // initialize fields for UI elemenets 
    App.mainBody = $("#main-body");
    App.topBanner = $("#top-banner");
    App.userGreeting = $("#user-greeting");
    App.login = $("#login");
    App.logout = $("#logout");
    App.viewAnonymous = $("#view-anonymous");
    App.viewUnassigned = $("#view-unassigned");
    App.loadingSpinner = $("#view-loading-spinner");
    App.loadingSpinnerMessage = $("#spinner-message");
    App.viewAuthenticated = $("#view-authenticated");
    App.viewAuthenticatedHeader = $("#view-authenticated-header");
    App.tenantName = $("#tenant-name");
    App.reportsList = $("#reports-list");
    App.datasetsList = $("#datasets-list");
    App.datasetsListContainer = $("#datasets-list-container");
    App.embedToolbar = $("#embed-toolbar");
    App.breadcrumb = $("#breadcrumb");
    App.toggleEditButton = $("#toggle-edit");
    App.fullScreenButton = $("#full-screen");
    App.embedContainer = $("#embed-container");
    App.brandBanner = $("#brand-banner");

    // set up authentication callback
    SpaAuthService.uiUpdateCallback = App.onAuthenticationCompleted;

    App.login.on("click", async () => {
      await SpaAuthService.login();
    });

    App.logout.on("click", () => {
      SpaAuthService.logout();
      App.refreshUi();
    });

    // Comment out to disable auto-authentication on startup
    SpaAuthService.attemptSillentLogin();

    App.refreshUi();

    App.registerWindowResizeHandler();
  }

  private static refreshUi = () => {

    if (SpaAuthService.userIsAuthenticated) {
      App.userGreeting.text("Welcome " + SpaAuthService.userDisplayName);
      App.userGreeting.prop('title', 'Email: ' + SpaAuthService.userName);
      App.login.hide()
      App.logout.show();
      App.viewAnonymous.hide();
    }
    else {
      App.userGreeting.text("");
      App.login.show();
      App.logout.hide();
      App.viewAnonymous.show();
      App.viewAuthenticated.hide();
    }
  }

  private static onAuthenticationCompleted = async () => {
    try {
      App.loadingSpinnerMessage.text("Processing user login...");
      App.loadingSpinner.show(250);
      App.viewAnonymous.hide();
      await AppOwnsDataWebApi.LoginUser(SpaAuthService.userName, SpaAuthService.userDisplayName);
      App.loadingSpinner.hide();
      App.refreshUi();
      App.initializeAppData();
    } catch (error) {
      console.error("Login processing failed:", error);
      App.loadingSpinner.hide();
      App.viewAnonymous.show();
    }
  }

  private static initializeAppData = async () => {
    try {
      App.loadingSpinnerMessage.text("Getting report embedding data...");
      App.loadingSpinner.show();
      App.viewAnonymous.hide();

      App.viewModel = await AppOwnsDataWebApi.GetEmbeddingData();

      if (App.viewModel.tenantName == "") {
        App.viewAnonymous.hide();
        App.viewAuthenticated.hide();
        App.loadingSpinner.hide();
        App.viewUnassigned.show(500);
      }
      else {
        console.log("Loading View Model", App.viewModel);
        App.loadViewModel(App.viewModel);
      }

      window.setInterval(App.reportOnExpiration, 10000);
    } catch (error) {
      console.error("Failed to load embedding data:", error);
      App.loadingSpinner.hide();
      App.viewUnassigned.show(500);
    }
  }

  private static loadViewModel = async (viewModel: ViewModel, reportId?: string) => {

    App.viewAuthenticated.hide();
    App.viewUnassigned.hide();

    App.powerbi.reset(App.embedContainer[0]);

    App.tenantName.text(viewModel.tenantName);
    App.applyBranding(viewModel);
    App.reportsList = App.reportsList.empty();
    App.datasetsList = App.datasetsList.empty();

    if (viewModel.reports.length == 0) {
      App.reportsList.append($("<li>")
        .text("no reports in workspace")
        .addClass("no-content"));
    }
    else {
      viewModel.reports.forEach((report: Report) => {
        var li = $("<li>");
        var iconCls = (report as any).reportType === "PaginatedReport" ? "fa-solid fa-file-lines" : "fa-solid fa-chart-bar";
        li.append(
          $("<a>", { "href": "javascript:void(0);" })
            .append($("<i>").addClass(iconCls))
            .append(document.createTextNode("\u00a0" + report.name))
            .click(function() {
              App.reportsList.find("li a").removeClass("active");
              $(this).addClass("active");
              App.embedReport(report);
            })
        );
        App.reportsList.append(li);
      });
    }
    
    if (viewModel.userCanCreate) {
      if (viewModel.datasets.length == 0) {
        App.datasetsList.append($("<li>")
          .text("no datasets in workspace")
          .addClass("no-content"));
      }
      else {
        viewModel.datasets.forEach((dataset: Dataset) => {
          var li = $("<li>");
          li.append(
            $("<a>", { "href": "javascript:void(0);" })
              .append($("<i>").addClass("fa-solid fa-database"))
              .append(document.createTextNode("\u00a0" + dataset.name))
              .click(() => { App.embedNewReport(dataset); })
          );
          App.datasetsList.append(li);
        });
      }
    }

    App.loadingSpinner.hide();
    App.viewAuthenticated.show();

    if (reportId !== undefined) {
      var newReport: Report = viewModel.reports.find((report) => report.id === reportId);
      App.embedReport(newReport, true);
      // mark matching link active
      App.reportsList.find("li a").filter((_, el) => $(el).text().trim() === newReport.name).addClass("active");
    }
    else {
      var newReport: Report = viewModel.reports[0];
      App.embedReport(newReport, false);
      // mark first link active
      App.reportsList.find("li:first-child a").addClass("active");
    }

  }

  private static embedReport = async (report: Report, editMode: boolean = false) => {

    App.setReportLayout();

    var models = pbimodels;

    var permissions;
    if (App.viewModel.userCanEdit && App.viewModel.userCanCreate) {
      permissions = models.Permissions.All;
    }
    else if (App.viewModel.userCanEdit && !App.viewModel.userCanCreate) {
      permissions = models.Permissions.ReadWrite;
    }
    else if (!App.viewModel.userCanEdit && App.viewModel.userCanCreate) {
      permissions = models.Permissions.Copy;
    }
    else if (!App.viewModel.userCanEdit && !App.viewModel.userCanCreate) {
      permissions = models.Permissions.Read;
    }

    App.setLayoutMode();
    var layoutMode: pbimodels.LayoutType =
      App.layoutMode == "master" ?
        models.LayoutType.Master :
        models.LayoutType.MobilePortrait;

    var config: powerbi.IReportEmbedConfiguration = {
      type: 'report',
      id: report.id,
      embedUrl: report.embedUrl,
      accessToken: App.viewModel.embedToken,
      tokenType: models.TokenType.Embed,
      permissions: permissions,
      viewMode: editMode ? models.ViewMode.Edit : models.ViewMode.View,
      settings: {
        layoutType: layoutMode,
        background: models.BackgroundType.Transparent,
        panes: {
          filters: { visible: false },
          pageNavigation: { visible: true, position: models.PageNavigationPosition.Left }
        }
      }
    };

    App.powerbi.reset(App.embedContainer[0]);

    // ── Size the container BEFORE embed so the PBI SDK reads the correct
    // clientHeight. The SDK locks in the iframe height at embed() time and
    // will not resize it later even if CSS changes. ──────────────────────────
    App.resizeEmbedContainer();

    var timerStart: number = Date.now();
    var initialLoadComplete: boolean = false;
    var loadDuration: number;
    var renderDuration: number;
    App.currentReport = <powerbi.Report>App.powerbi.embed(App.embedContainer[0], config);

    App.currentReport.off("loaded")
    App.currentReport.on("loaded", async (event: any) => {
      loadDuration = Date.now() - timerStart;
      App.setReportLayout();
      App.applyPowerBiTheme(App.currentReport, App.viewModel);
    });

    App.currentReport.off("rendered");
    App.currentReport.on("rendered", async (event: any) => {

      if (!initialLoadComplete) {
        renderDuration = Date.now() - timerStart;
        var correlationId: string = await App.currentReport.getCorrelationId();
        await App.logViewReportActivity(correlationId, App.viewModel.embedTokenId, report, loadDuration, renderDuration);
        initialLoadComplete = true;
      }

    });

    App.currentReport.off("saved");
    App.currentReport.on("saved", async (event: any) => {
      if (event.detail.saveAs) {
        console.log("SaveAs Event", event);
        var orginalReportId = report.id;
        var reportId: string = event.detail.reportObjectId;
        var reportName: string = event.detail.reportName;
        await App.logCopyReportActivity(report, reportId, reportName, App.viewModel.embedTokenId);
        App.viewModel = await AppOwnsDataWebApi.GetEmbeddingData();
        App.loadViewModel(App.viewModel, reportId);
      }
      else {
        console.log("Save Event", event);
        await App.logEditReportActivity(report, App.viewModel.embedTokenId);
      }
    });

    var viewMode = editMode ? "edit" : "view";

    App.breadcrumb.text("Reports > " + report.name);    

    if (!App.viewModel.userCanEdit || report.reportType != "PowerBIReport") {
      console.log("Hiding toggle edit");
      App.toggleEditButton.hide();
    }
    else {
      App.toggleEditButton.show();
      App.toggleEditButton.on("click", () => {
        // toggle between view and edit mode
        viewMode = (viewMode == "view") ? "edit" : "view";
        App.currentReport.switchMode(viewMode);
        // show filter pane when entering edit mode
        var showFilterPane = (viewMode == "edit");
        App.currentReport.updateSettings({
          panes: {
            filters: { visible: showFilterPane, expanded: false }
          }
        });
      });
    }

    App.fullScreenButton.on("click", () => {
      App.currentReport.fullscreen();
    });

  }

  private static embedNewReport = (dataset: Dataset) => {

    var models = pbimodels;

    var config: powerbi.IEmbedConfiguration = {
      datasetId: dataset.id,
      embedUrl: "https://app.powerbi.com/reportEmbed",
      accessToken: App.viewModel.embedToken,
      tokenType: models.TokenType.Embed,
      settings: {
        panes: {
          filters: { visible: true, expanded: false }
        }
      }
    };


    // Embed the report and display it within the div container.
    App.powerbi.reset(App.embedContainer[0]);
    var embeddedReport = App.powerbi.createReport(App.embedContainer[0], config);

    $("#breadcrumb").text("Datasets > " + dataset.name + " > New Report");
    $("#embed-toolbar").show();

    $("#toggle-edit").hide();
    $("#full-screen").off("click");
    $("#full-screen").on("click", () => {
      embeddedReport.fullscreen();
    });

    // handle save action on new report
    embeddedReport.on("saved", async (event: any) => {
      console.log("Create Report Event", event);
      var reportId: string = event.detail.reportObjectId;
      var reportName: string = event.detail.reportName;
      await App.logCreateReportActivity(dataset, reportId, reportName, App.viewModel.embedTokenId);
      App.viewModel = await AppOwnsDataWebApi.GetEmbeddingData();
      App.loadViewModel(App.viewModel, reportId);
    });

  };

  private static setLayoutMode = () => {
    let useMobileLayout: boolean = (App.mainBody.width() < App.breakPointWidth);
    App.layoutMode = useMobileLayout ? "mobile" : "master";
  };

  // ── Resize the embed container to fill available viewport height ──────────
  // Must be called BEFORE powerbi.embed() — the SDK reads clientHeight once
  // at embed time and never resizes itself.
  private static resizeEmbedContainer = () => {
    const winH   = $(window).height()  || window.innerHeight;
    const topH   = App.topBanner[0]   ? App.topBanner[0].offsetHeight   : 0;
    const banH   = App.brandBanner[0] ? App.brandBanner[0].offsetHeight : 0;
    const hdrH   = App.viewAuthenticatedHeader[0] ? App.viewAuthenticatedHeader[0].offsetHeight : 0;
    const height = Math.max(winH - topH - banH - hdrH - 8, 300);
    App.embedContainer.css({ height: height + 'px', 'min-height': '300px' });
  };

  private static setReportLayout = async () => {

    let useMobileLayout: boolean = (App.mainBody.width() < App.breakPointWidth);
    // check to see if layout mode switches between master and mobile
    if ((useMobileLayout && App.layoutMode == "master") ||
      (!useMobileLayout && App.layoutMode == "mobile")) {
     if (App.currentReport) {
        console.log("switching layout mode...")
        App.layoutMode = useMobileLayout ? "mobile" : "master";
        let sameReport: Report = App.viewModel.reports.find(async (report) => report.id === await App.currentReport.getId());
        App.embedReport(sameReport);
      }
    }
    else {
      var models = pbimodels;
      if (useMobileLayout) {
        App.tenantName.hide();
        App.toggleEditButton.hide();
        App.fullScreenButton.hide();
        App.datasetsListContainer.hide();
        $(App.embedContainer).height($(App.embedContainer).width() * 3);
      }
      else {
        // CSS flex layout (height: calc(100vh - 196px) on #embed-layout)
        // handles the container height exactly like the admin portal.
        // Also resize the container so any already-embedded report updates.
        App.tenantName.show();
        App.fullScreenButton.show();
        if (App.viewModel && App.viewModel.userCanCreate) {
          App.datasetsListContainer.show();
        }
        else {
          App.datasetsListContainer.hide();
        }
        App.resizeEmbedContainer();
        if (App.currentReport) {
          App.currentReport.reload();
        }
      }
    }

  };

  private static logViewReportActivity = async (correlationId: string, embedTokenId: string, report: Report, loadDuration: number, renderDuration) => {
    var logEntry: ActivityLogEntry = new ActivityLogEntry();
    logEntry.CorrelationId = correlationId;
    logEntry.EmbedTokenId = embedTokenId;
    logEntry.Activity = "ViewReport";
    logEntry.LoginId = App.viewModel.user;
    logEntry.Tenant = App.viewModel.tenantName;
    logEntry.Report = report.name;
    logEntry.ReportId = report.id;
    logEntry.DatasetId = report.datasetId;
    logEntry.Dataset = (App.viewModel.datasets.find((dataset) => dataset.id === report.datasetId)).name;
    logEntry.LoadDuration = loadDuration;
    logEntry.RenderDuration = renderDuration;
    await AppOwnsDataWebApi.LogActivity(logEntry);
  };

  private static logEditReportActivity = async (report: Report, embedTokenId: string) => {
    var logEntry: ActivityLogEntry = new ActivityLogEntry();
    logEntry.CorrelationId = "";
    logEntry.Activity = "EditReport";
    logEntry.LoginId = App.viewModel.user;
    logEntry.Tenant = App.viewModel.tenantName;
    logEntry.Report = report.name;
    logEntry.ReportId = report.id;
    logEntry.DatasetId = report.datasetId;
    logEntry.EmbedTokenId = embedTokenId;
    logEntry.Dataset = (App.viewModel.datasets.find((dataset) => dataset.id === report.datasetId)).name;
    await AppOwnsDataWebApi.LogActivity(logEntry);
  };

  private static logCopyReportActivity = async (orginalReport: Report, reportId: string, reportName, embedTokenId: string) => {
    var logEntry: ActivityLogEntry = new ActivityLogEntry();
    logEntry.Activity = "CopyReport";
    logEntry.LoginId = App.viewModel.user;
    logEntry.Tenant = App.viewModel.tenantName;
    logEntry.Report = reportName;
    logEntry.ReportId = reportId;
    logEntry.OriginalReportId = orginalReport.id;
    logEntry.DatasetId = orginalReport.datasetId;
    logEntry.EmbedTokenId = embedTokenId;
    logEntry.Dataset = (App.viewModel.datasets.find((dataset) => dataset.id === orginalReport.datasetId)).name;
    await AppOwnsDataWebApi.LogActivity(logEntry);
  };

  private static logCreateReportActivity = async (dataset: Dataset, reportId: string, reportName, embedTokenId: string) => {
    var logEntry: ActivityLogEntry = new ActivityLogEntry();
    logEntry.Activity = "CreateReport";
    logEntry.LoginId = App.viewModel.user;
    logEntry.Tenant = App.viewModel.tenantName;
    logEntry.Report = reportName;
    logEntry.ReportId = reportId;
    logEntry.DatasetId = dataset.id;
    logEntry.Dataset = dataset.name;
    logEntry.EmbedTokenId = embedTokenId;
    await AppOwnsDataWebApi.LogActivity(logEntry);
  };

  private static registerWindowResizeHandler = async () => {
    $(window).resize(async function () {
      clearTimeout(App.resizedFinished);
      App.resizedFinished = setTimeout(async function () {
        App.setReportLayout();
      }, 100);
    });
  }

  private static buildLogoSvg = (sym: string): string => {
    sym = (sym || 'T').toUpperCase();
    const logos: { [key: string]: string } = {
      'WT': '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 44 44"><circle cx="22" cy="22" r="21" fill="none" stroke="rgba(255,255,255,0.2)" stroke-width="1"/><path d="M5 18 C14 11 30 9 41 14 L37 21 C26 17 12 19 7 25 Z" fill="rgba(255,255,255,0.95)"/><path d="M7 25 C11 27 17 26 21 23 L19 30 C13 32 6 30 7 27 Z" fill="rgba(255,255,255,0.55)"/><line x1="22" y1="28" x2="22" y2="39" stroke="rgba(255,255,255,0.6)" stroke-width="2" stroke-linecap="round"/><circle cx="38" cy="10" r="2.5" fill="rgba(255,255,255,0.4)"/></svg>',
      'CO': '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 44 44"><circle cx="22" cy="22" r="21" fill="none" stroke="rgba(255,255,255,0.2)" stroke-width="1"/><path d="M35 13 A17 17 0 1 0 35 31 L29 27 A10 10 0 1 1 29 17 Z" fill="rgba(255,255,255,0.95)"/><path d="M30 18 L37 14 L36 22 L30 26 Z" fill="rgba(255,255,255,0.35)"/><circle cx="22" cy="22" r="2.5" fill="rgba(255,255,255,0.25)"/></svg>',
      'MC': '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 44 44"><circle cx="22" cy="22" r="21" fill="none" stroke="rgba(255,255,255,0.2)" stroke-width="1"/><rect x="5" y="30" width="6" height="9" rx="1.5" fill="rgba(255,255,255,0.65)"/><rect x="13" y="22" width="6" height="17" rx="1.5" fill="rgba(255,255,255,0.8)"/><rect x="21" y="13" width="6" height="26" rx="1.5" fill="rgba(255,255,255,0.95)"/><rect x="29" y="19" width="6" height="20" rx="1.5" fill="rgba(255,255,255,0.75)"/><line x1="24" y1="13" x2="24" y2="6" stroke="rgba(255,255,255,0.7)" stroke-width="1.5" stroke-linecap="round"/><circle cx="24" cy="5" r="2" fill="rgba(255,255,255,0.6)"/></svg>',
      'AC': '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 44 44"><circle cx="22" cy="22" r="21" fill="none" stroke="rgba(255,255,255,0.2)" stroke-width="1"/><path d="M22 7 L34 36 L22 30 L10 36 Z" fill="none" stroke="rgba(255,255,255,0.95)" stroke-width="2.5" stroke-linejoin="round" stroke-linecap="round"/><line x1="15" y1="27" x2="29" y2="27" stroke="rgba(255,255,255,0.95)" stroke-width="2" stroke-linecap="round"/><circle cx="22" cy="17" r="2" fill="rgba(255,255,255,0.75)"/><circle cx="36" cy="11" r="3.5" fill="none" stroke="rgba(255,255,255,0.45)" stroke-width="1.5"/><circle cx="36" cy="11" r="1.2" fill="rgba(255,255,255,0.6)"/></svg>',
      'ROCKET': '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 44 44"><circle cx="22" cy="22" r="21" fill="none" stroke="rgba(255,255,255,0.2)" stroke-width="1"/><path d="M16 14 Q22 3 28 14 Z" fill="rgba(255,255,255,0.95)"/><rect x="16" y="14" width="12" height="16" rx="2" fill="rgba(255,255,255,0.9)"/><circle cx="22" cy="19" r="3" fill="none" stroke="rgba(255,255,255,0.45)" stroke-width="1.5"/><path d="M16 24 L9 34 L16 30 Z" fill="rgba(255,255,255,0.72)"/><path d="M28 24 L35 34 L28 30 Z" fill="rgba(255,255,255,0.72)"/><path d="M16 30 L14 34 L30 34 L28 30 Z" fill="rgba(255,255,255,0.55)"/><path d="M14 34 C13 38 22 43 22 43 C22 43 31 38 30 34 Z" fill="rgba(255,255,255,0.85)"/><path d="M17 34 C16 37 22 40 22 40 C22 40 28 37 27 34 Z" fill="rgba(255,255,255,0.3)"/></svg>'
    };
    if (logos[sym]) return logos[sym];
    const fs = sym.length > 1 ? '13' : '17';
    return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 44 44"><circle cx="22" cy="22" r="21" fill="none" stroke="rgba(255,255,255,0.3)" stroke-width="1.5"/><circle cx="22" cy="22" r="17" fill="rgba(255,255,255,0.1)"/><text x="22" y="28" text-anchor="middle" font-family="Inter,system-ui,sans-serif" font-size="${fs}" font-weight="800" fill="white" letter-spacing="-0.5">${sym}</text></svg>`;
  };

  private static applyBranding = (viewModel: ViewModel) => {
    const t = viewModel.theme;
    if (!t) return;
    const primary   = t.primary   || '#37474F';
    const secondary = t.secondary || '#78909C';
    const tertiary  = t.tertiary  || '#FF6F00';
    document.documentElement.style.setProperty('--theme-primary',   primary);
    document.documentElement.style.setProperty('--theme-secondary', secondary);
    document.documentElement.style.setProperty('--theme-tertiary',  tertiary);
    App.brandBanner.css('background', `linear-gradient(135deg, ${primary} 0%, ${secondary} 100%)`);
    const sym = t.logoSymbol || viewModel.tenantName.substring(0, 2).toUpperCase();
    $("#brand-logo").html(App.buildLogoSvg(sym));
    $("#brand-company-name").text(viewModel.tenantName);
    $("#brand-tagline").text(t.tagline || '');
  };

  private static applyPowerBiTheme = (report: powerbi.Report, viewModel: ViewModel) => {
    if (!viewModel?.theme) return;
    const t = viewModel.theme;
    const themeJson = {
      name: viewModel.tenantName + ' Theme',
      dataColors: [
        t.primary, t.secondary, t.tertiary,
        '#546E7A', '#90A4AE', '#FF8F00', '#00897B', '#5C6BC0', '#F06292'
      ],
      tableAccent: t.primary,
      foreground: '#212121',
      background: '#FFFFFF'
    };
    report.applyTheme({ themeJson }).catch(e => console.warn('PBI theme apply failed:', e));
  };

  private static reportOnExpiration = async () => {
    var secondsToExpire = Math.floor((new Date(App.viewModel.embedTokenExpiration).getTime() - new Date().getTime()) / 1000);
    var minutes = Math.floor(secondsToExpire / 60);
    var seconds = secondsToExpire % 60;
    var timeToExpire = minutes + ":" + seconds;
    console.log("Token expires in ", timeToExpire);
  };

}

$(App.onDocumentReady);
