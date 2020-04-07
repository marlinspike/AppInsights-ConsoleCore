# Application Insights in a .NET Core Console App
This app demos using Application Insights in a Console app, with several examples of various API usages. Also
included are examples of **dependency tracking** with a Python and HTTP Request.

If you want to remove the Python depdendency, just comment out the line that calls the Python script.

## Requirements
### Azure Application Insights Resource
1. Create an Application Insights Resource in Azure
2. Note down it's Instrumentation Key, as you'll need it in the next step

### appsettings file
1. Add an **appsettings.json** file in your root directory with the following contents:

```
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "MySettings": {
    "InstrumentationKey": "<Your App Insights Instrumentation key>",
    "UserId": "<Any Id you want to mock>"
  }
}
```

### Python
Python 3.6+



