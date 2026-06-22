<img align="left" width="80" height="80" src="https://user-images.githubusercontent.com/16055659/169651886-f53360a0-ccad-49f0-8c68-d8f0d5306ecc.png" alt="app_icon">

# EventLook
<br/>

> [!NOTE]
> EventLook on Microsoft Store has been reimagined with Pro features added — [see below for details](#microsoft-store-version).
<br/>

![screenshot](/Screenshot-1.png)

# Overview
The inbox Windows Event Viewer is a great app that provides comprehensive functionalities in examining events. However, the user experience is not as good as I wish in some usage scenarios - for example, as the list view does not provide a preview for event messages, it would not be suitable to overview what was happening in the machine. 
**EventLook** aims to offer an alternative when you want to quickly examine Event Logs, with such UX issues addressed. 

# Features
- Overview events with Event Log messages
- Asynchronous event fetching for quick glance
- Provides quicker sort, specifying time range, and filters
- Supports auto refresh with new events highlighted
- Provides access to all Event Logs in local machine, including Applications and Services Logs
- Supports .evtx file (open from Explorer or drag & drop .evtx file)
- Double click to view event details in XML format
- Right click to quickly filter events
- Adjust time of events by time zone (Useful when you investigate .evtx file from different time zone)

# Technologies
- WPF on .NET 8
- The basic structure for DataGrid with filters is based on an article in CodeProject: [Multi-filtered WPF DataGrid with MVVM](https://www.codeproject.com/Articles/442498/Multi-filtered-WPF-DataGrid-with-MVVM)
- [MVVM Toolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Extended WPF Toolkit](https://github.com/xceedsoftware/wpftoolkit)
- Reading event logs are done by APIs in [System.Diagnostics.Eventing.Reader](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.eventing.reader?view=netframework-4.8) namespace. The API provides a unified access to logs both from the legacy [Event Logging](https://docs.microsoft.com/en-us/windows/win32/eventlog/event-logging) and the modern [Windows Event Log](https://docs.microsoft.com/en-us/windows/win32/wes/windows-event-log) infrastructure.

---

# Microsoft Store Version

Going independent as a developer gave me the opportunity to rebuild EventLook on top of this codebase (the OSS version). The Store version includes the following additions and many other improvements. All features from the OSS version remain freely available; select features are offered as a one-time **Pro** upgrade.

- UI Redesign
  - A modern, clean look with support for multiple languages and dark mode.
- Activity Chart (Pro)
  - Visualize event patterns with an intuitive chart. Double-click to jump to an event, or drag to zoom.
- AI Assistant (Pro)
  - Get expert AI-powered analysis of event logs.

👉 [**Try it on the Microsoft Store**](https://www.microsoft.com/store/apps/9NJV5FQ089Z0)

Feedback is always welcome via [Issues](https://github.com/kmaki565/EventLook/issues) and [Discussions](https://github.com/kmaki565/EventLook/discussions) here :wink:
