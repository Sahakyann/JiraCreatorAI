using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

public class GeminiService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient = new();
    private const string GeminiProModel = "gemini-1.5-pro-latest";
    private const string ApiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/";

    public GeminiService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<string> GenerateTicketAsync(string input, string? stepsJson = null)
    {
        var currentDate = DateTime.UtcNow.ToString("MMMM dd, yyyy");

        string formattedSteps = "";
        if (!string.IsNullOrEmpty(stepsJson))
        {
            try
            {
                var stepsList = new List<string>();
                var stepsJsonDoc = JsonDocument.Parse(stepsJson);

                var commands = stepsJsonDoc.RootElement.GetProperty("commands");

                int stepNumber = 1;
                foreach (var cmd in commands.EnumerateArray())
                {
                    string action = cmd.GetProperty("command").GetString() ?? "";
                    string target = cmd.GetProperty("target").GetString() ?? "";
                    string value = cmd.TryGetProperty("value", out var valProp) ? valProp.GetString() ?? "" : "";

                    string humanStep = action switch
                    {
                        "open" => $"Navigate to {target}",
                        "click" => $"Click on element '{target}'",
                        "type" => $"Type '{value}' into element '{target}'",
                        "select" => $"Select '{value}' from dropdown '{target}'",
                        _ => $"Perform '{action}' on '{target}' {value}".Trim()
                    };

                    stepsList.Add($"{stepNumber++}. {humanStep}");
                }

                formattedSteps = string.Join("\n", stepsList);
            }
            catch (Exception ex)
            {
                formattedSteps = "Error parsing uploaded steps.";
            }
        }

        var stepsText = string.IsNullOrEmpty(formattedSteps) ? "(Steps based on description below)" : formattedSteps;

        var promptText = @$"
You are an assistant that generates Jira tickets for ServiceTitan's 'Inventory' project.

Instructions:
- Follow the structure carefully.
- Write using full sentences and complete paragraphs.
- Expand explanations where possible.
- Assume the reader is a QA engineer or developer unfamiliar with the issue.
- Match the style shown in examples when provided.
- Maintain clear formatting and a professional tone.
- If a section is missing information, make reasonable clarifications but do not invent false data.

Template:

What is the issue?: 
(Provide a detailed paragraph explaining the core issue.)

Name(s) of initial tenant(s) impacted: 
(If mentioned. Otherwise leave blank.)

Steps to reproduce:
{stepsText}

Expected result:
(Describe what should ideally happen.)

Actual result:
(Describe what actually happens.)

What troubleshooting did you do to resolve this issue?:
(Explain any validation, testing, tenant replication you attempted.)

SQL Query to check inventory tracking (optional):
(Include if available.)

Full Story Link:
(Add links if provided.)

What time did it occur?: {currentDate}

What is the technician name(s)?:
(If available.)

What is the job number?:
(If available.)

Preconditions (feature gates, etc.):
(Include if mentioned.)

Links to supporting docs/Jira tasks:
(Add if available.)

Strategic Importance (OB, CSM entry):
(If mentioned.)


---
What is the issue?:

Scenario #1.1 (location selected on the Invoice Item):
Whenever a PO item is copied from the job invoice (items on the invoice HAVE an inventory location) ([SourceInvoiceItemId] is not null on [dbo].[PurchaseOrderItem]) and later the item is deleted from the invoice, an error pops up when trying to create a Return with the job association.

🎬 Loom Video Recording: Return Creation for PO #2664548-001


Scenario #1.2 (location NOT selected on the Invoice Item):
Whenever a PO item is copied from the job invoice (items on the invoice DO NOT HAVE an inventory location / No tech is assigned to the job) ([SourceInvoiceItemId] is not null on [dbo].[PurchaseOrderItem]) and later the item is deleted from the invoice, an error pops up when trying to create a Return with the job association.

🎬 Loom Video Recording: recording link 1

Scenario #2 (Non-Inventory items):
In certain scenarios (e.g., when the item is copied to the invoice upon receiving the Purchase Order), the same error occurs under the following conditions:

The Purchase Order (PO) is received with Item X (Quantity: 2).

The PO item is added to the job invoice upon receiving (Quantity: 2).

A return is created for the PO from the job, but the quantity on the return is entered as less than the PO quantity (Quantity: 1).

The return is created without any issues. However, when attempting to edit the return, the same error appears.
🎬 Loom Video Recording: Return Edit for PO #2665562-002

In both scenarios, we get the same error:

Action cancelled

XXX qty return should be less than X for return

XXX of the return should be the same as the location of the invoice item.''



Name(s) of initial tenant(s) impacted: 
myguyplumbing


Steps to reproduce:
Scenario #1.1

Book a Job > Add a Material/Equipment item to the job

Navigate to the Job Invoice page > Add a PO

Click on “Copy Invoice Items”

Create > Send > Receive the PO

Navigate to the job > Complete the job

Navigate to Invoice > Add a Return (to Vendor)

Select the PO > Try to Create

Action cancelled: XXX qty return should be less than 0 for return



Scenario #1.2

Book a Job > Add a Material/Equipment item to the job

Navigate to the Job Invoice page > Add a PO

Click on “Copy Invoice Items”

Create > Send > Receive the PO

Navigate to the job > Cancel the Job

Navigate to Invoice > Add a Return (to Vendor)

Remove the Job Number > Select the PO > Try to Create

Action cancelled: Location XXX of the return should be the same as the location of the invoice item.''

Scenario #2:

Navigate to any job

Add a PO > Add a non-inventory item with Qty 2

Send and Receive the PO

Click “Yes” on the Copy to Invoice pop-up

PO items are added to the job invoice

Navigate to the invoice > Add a Return (to Vendor)

Select the PO > edit the Return Qty from 2 to 1 > Create

Return is normally created

Navigate to the Return page > Try to Edit and save

Action cancelled: XXX qty return should be less than 1 for return

Expected result:

All Scenarios: Return should be created without an error.


Actual result:

Scenario #1.1: Action cancelled error pops up when the PO item is deleted from the invoice and a Return is being created.

Scenario #1.2: Action cancelled error pops up when the PO item is deleted from the invoice, Job Number is removed from the Return and a Return is being created.

Scenario #2: Action canceled error pops up when editing Vendor Return created with less Qty than the original PO Qty




What troubleshooting did you do to resolve this issue?
The issue is replicable in any account.

Important Note: 
Scenario #1.1 and Scenario #1.2

This issue is not replicable when using the “Copy to Invoice” or Rate Sheet workflows. In these cases invoice items are associated with PO items on [dbo].[InvoiceItem] — SourceInvoiceItemId is empty on [dbo].[PurchaseOrderItem] .  

In other words, only when the [SourceInvoiceItemId] is NOT empty and the item is deleted from the invoice, an error pops up upon creation.

Scenario #2
This issue is replicable no matter how the item was added to the job and no matter it was deleted or even not touched. When editing the Returns, seems the system treats the “Save” button as an action of a new return generation.


Demo Tenant: hrayrelectrical
🎬 Loom Recording: Return Creation for PO #2664548-001
🎬 Loom Recording: Return Edit for PO #2665562-002




Workaround:
Scenario #1.1
When removing the Job Number from the Return, no error pops up. Since the PO is still associated with the job, creating a return with only PO association completely resolves the issue --- EASY

Scenario #1.2
No workaround from the UI.


Scenario #2
Cancel the Return and create a new Return instead (if anything should be edited such as Project Label, tax, etc.) --- MEDIUM


Full Story Link:
-

What time did it occur?:
December 30, 2024

What is the technician name(s)?:
-

What is the job number?:
-

Preconditions (feature gates, etc.):
Purchasing Module FG ON

Links to supporting docs/Jira tasks:
link to supporting ticket

Strategic Importance (OB, CSM entry):
-

Customer Pain Index: 
-

---
Input:
{input}
";

        var prompt = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = promptText }
                    }
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(prompt), Encoding.UTF8, "application/json");
        var requestUri = $"{ApiEndpoint}{GeminiProModel}:generateContent";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Add("x-goog-api-key", _config["GeminiApiKey"]);
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            return "Error: Rate limit exceeded. Please wait a few minutes and try again.";
        }

        if (response.IsSuccessStatusCode)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "Error: Empty response.";
            }
            catch (JsonException ex)
            {
                return $"Error parsing JSON response: {ex.Message}";
            }
        }
        else
        {
            return $"Error generating ticket. Status Code: {response.StatusCode}, Reason: {response.ReasonPhrase}, Content: {json}";
        }
    }

    public async Task<string> ListAvailableModelsAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://generativelanguage.googleapis.com/v1beta/models");
        request.Headers.Add("x-goog-api-key", _config["GeminiApiKey"]);

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        return $"List Models Response (Status: {response.StatusCode}): {json}";
    }
}
