<img align="left" width="80" height="80" src="https://user-images.githubusercontent.com/16055659/169651886-f53360a0-ccad-49f0-8c68-d8f0d5306ecc.png" alt="app_icon">

# EventLook

| :point_up: EventLook is now available in [Microsoft Store](https://www.microsoft.com/store/apps/9NJV5FQ089Z0)! |
|-----|

A fast & handy alternative to Windows Event Viewer built on WPF

![screenshot](/Screenshot-1.png)

# Overview
The inbox Windows Event Viewer is a great app that provides comprehensive functionalities in examining events. However, the user experience is not as good as I wish in some usage scenarios - for example, as the list view does not provide a preview for event messages, it would not be suitable to overview what was happening in the machine. 
**EventLook** aims to offer an alternative when you want to quickly examine Event Logs, with such UX issues addressed. 

# Features
- Overview events with Event Log messages
- Asynchronous event fetching for quick glance
- Provides quicker sort, specifying range, filters, and search (coming soon) 
- Quick access to your favorite Applications and Services Logs
- View other machine's event logs from evtx file (drag & drop .evtx and choose it in the drop-down of event sources)

# Install
Just download the latest artifact, unzip it, and run EventLook.exe. No registry/local storage is used as of now.

# Technologies
- The basic structure for DataGrid with filters is based on an article in CodeProject: [Multi-filtered WPF DataGrid with MVVM](https://www.codeproject.com/Articles/442498/Multi-filtered-WPF-DataGrid-with-MVVM)
- [MvvmLight](http://www.mvvmlight.net/)
- [Extended WPF Toolkit](https://github.com/xceedsoftware/wpftoolkit)
- Reading event logs are done by APIs in [System.Diagnostics.Eventing.Reader](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.eventing.reader?view=netframework-4.8) namespace. The API provides a unified access to logs both from the legacy [Event Logging](https://docs.microsoft.com/en-us/windows/win32/eventlog/event-logging) and the modern [Windows Event Log](https://docs.microsoft.com/en-us/windows/win32/wes/windows-event-log) infrastructure.

# TODO
In addition to `TODO` embedded in the code, wished features are listed up in Issues. I'd be happy if you could join the development!

# Contribution
1. Fork it to your account
1. Create your feature branch (git checkout -b my-new-feature)
1. Commit your changes (git commit -am 'Add some feature')
1. Push to the branch (git push origin my-new-feature)
1. Create new Pull Request