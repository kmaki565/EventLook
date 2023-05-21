<img align="left" width="80" height="80" src="https://user-images.githubusercontent.com/16055659/169651886-f53360a0-ccad-49f0-8c68-d8f0d5306ecc.png" alt="app_icon">

# EventLook

| :point_up: EventLook is now available in [Microsoft Store](https://www.microsoft.com/store/apps/9NJV5FQ089Z0)! |
|-----|

A fast & handy alternative to Windows Event Viewer built on WPF and .NET 6

![screenshot](/Screenshot-1.png)

# Overview
The inbox Windows Event Viewer is a great app that provides comprehensive functionalities in examining events. However, the user experience is not as good as I wish in some usage scenarios - for example, as the list view does not provide a preview for event messages, it would not be suitable to overview what was happening in the machine. 
**EventLook** aims to offer an alternative when you want to quickly examine Event Logs, with such UX issues addressed. 

# Features
- Overview events with Event Log messages
- Asynchronous event fetching for quick glance
- Provides quicker sort, specifying time range, and filters
- Provides access to all Event Logs in local machine, including Applications and Services Logs
- Supports .evtx file (open from Explorer or drag & drop .evtx file)
- Double click to view event details in XML format
- Right click to quickly filter events
- Adjust time of events by time zone (Useful when you investigate .evtx file from different time zone)

# Install
Install from [Microsoft Store](https://www.microsoft.com/store/apps/9NJV5FQ089Z0).
You could also download the latest artifact from [Actions](https://github.com/kmaki565/EventLook/actions), unzip it, and run EventLook.exe. No registry/local storage is used as of now.

# Technologies
- The basic structure for DataGrid with filters is based on an article in CodeProject: [Multi-filtered WPF DataGrid with MVVM](https://www.codeproject.com/Articles/442498/Multi-filtered-WPF-DataGrid-with-MVVM)
- [MVVM Toolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Extended WPF Toolkit](https://github.com/xceedsoftware/wpftoolkit)
- Reading event logs are done by APIs in [System.Diagnostics.Eventing.Reader](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.eventing.reader?view=netframework-4.8) namespace. The API provides a unified access to logs both from the legacy [Event Logging](https://docs.microsoft.com/en-us/windows/win32/eventlog/event-logging) and the modern [Windows Event Log](https://docs.microsoft.com/en-us/windows/win32/wes/windows-event-log) infrastructure.

# Contribute
We welcome your code! A standard contribution procedure is as follows:
1. Fork it to your account
1. Create your feature branch (git checkout -b my-new-feature)
1. Commit your changes (git commit -am 'Add some feature')
1. Push to the branch (git push origin my-new-feature)
1. Create a new Pull Request

If you have a desired feature, please let me know by opening a new issue :wink: