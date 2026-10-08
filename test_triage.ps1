$ErrorActionPreference = 'Stop'
$baseUrl = 'http://localhost:5014/api/v1'

Write-Host "Logging in..."
$loginBody = @{
    email = 'kasun.fernando@patient.carepulse.dev'
    password = 'Patient@12345'
} | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType 'application/json'
$token = $loginResponse.token

Write-Host "Getting profile..."
$profileHeaders = @{ Authorization = "Bearer $token" }
$profileResponse = Invoke-RestMethod -Uri "$baseUrl/patients/me" -Method Get -Headers $profileHeaders
$patientId = $profileResponse.id

function Test-Triage($symptoms, $severity) {
    Write-Host "Testing Symptoms: $symptoms"
    $triageBody = @{
        patientProfileId = $patientId
        symptoms = $symptoms
        duration = 'Since this morning'
        severity = $severity
        additionalSymptoms = @()
        hasPhotoAttachment = $false
    } | ConvertTo-Json -Depth 5

    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/triage/submit" -Method Post -Headers $profileHeaders -Body $triageBody -ContentType 'application/json'
        Write-Host "Result:"
        $response | ConvertTo-Json -Depth 5
    } catch {
        Write-Host "Error:"
        $_.Exception.Response | fl
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $reader.ReadToEnd()
    }
    Write-Host "--------------------------------"
}

Test-Triage "Mild headache" "Mild"
Test-Triage "Fever and headache" "Moderate"
Test-Triage "Severe chest pain and breathing difficulty" "Severe"
