$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5187/api'
$recruiterToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjZhMGIzZTM3ZWExNDM0NTMzZDRkZTE5ZSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6ImhudC52bi52bkBnbWFpbC5jb20iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiVGhp4buHbiIsInJvbGUiOiJyZWNydWl0ZXIiLCJleHAiOjE3NzkyMDg2MjIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTE4NyIsImF1ZCI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTE3MyJ9.XbgaY_4ZJTkm5v-qEKeZ7BFpCAonyBeTwcl48c3dVB8'
$results = @()

function Add-Result($name, $ok, $detail) {
  $script:results += [pscustomobject]@{
    Test = $name
    Pass = $ok
    Detail = $detail
  }
}

function Invoke-Api($name, [scriptblock]$script) {
  try {
    $res = & $script
    $detail = if ($null -eq $res) { 'OK (no body)' } else { $res | ConvertTo-Json -Compress -Depth 6 }
    Add-Result $name $true $detail
    return $res
  } catch {
    $code = 'ERR'
    try { $code = $_.Exception.Response.StatusCode.value__ } catch {}
    $body = ''
    try {
      $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
      $body = $reader.ReadToEnd()
    } catch {}
    Add-Result $name $false "$code $body"
    return $null
  }
}

$recruiterHeaders = @{ Authorization = "Bearer $recruiterToken" }

Invoke-Api 'Auth Me (recruiter token)' { Invoke-RestMethod -Uri "$base/auth/me" -Method Get -Headers $recruiterHeaders } | Out-Null
Invoke-Api 'Get Categories (public)' { Invoke-RestMethod -Uri "$base/categories" -Method Get } | Out-Null
Invoke-Api 'Upsert Company (empty category -> null)' {
  Invoke-RestMethod -Uri "$base/companies" -Method Post -Headers $recruiterHeaders -Body @{
    name = 'BreakThrough Test Co'
    description = 'Smoke test company'
    website = 'https://example.com'
    categoryId = ''
  }
} | Out-Null

$myCompany = Invoke-Api 'Get My Company' { Invoke-RestMethod -Uri "$base/companies/my" -Method Get -Headers $recruiterHeaders }

$newJob = Invoke-Api 'Create Job (empty category)' {
  Invoke-RestMethod -Uri "$base/jobs" -Method Post -Headers $recruiterHeaders -ContentType 'application/json' -Body (@{
    title = 'Smoke Test Job'
    categoryId = ''
    description = 'Testing create'
    responsibilities = @('Build')
    mustHaveSkills = @('C#')
    niceToHaveSkills = @('Vue')
    minExperienceYears = 1
  } | ConvertTo-Json)
}

if ($newJob -and $newJob.id) {
  Invoke-Api 'Update Job' {
    Invoke-RestMethod -Uri "$base/jobs/$($newJob.id)" -Method Put -Headers $recruiterHeaders -ContentType 'application/json' -Body (@{
      title = 'Smoke Test Job Updated'
      categoryId = ''
      description = 'Testing update'
      responsibilities = @('Build', 'Review')
      mustHaveSkills = @('C#')
      niceToHaveSkills = @('Vue')
      minExperienceYears = 2
    } | ConvertTo-Json)
  } | Out-Null

  Invoke-Api 'Get Jobs by Company' {
    Invoke-RestMethod -Uri "$base/jobs/company/$($newJob.companyId)" -Method Get
  } | Out-Null
}

Invoke-Api 'Get Jobs (public)' { Invoke-RestMethod -Uri "$base/jobs" -Method Get } | Out-Null

$candidateLogin = Invoke-Api 'Update Role -> candidate' {
  Invoke-RestMethod -Uri "$base/auth/update-role" -Method Put -Headers $recruiterHeaders -ContentType 'application/json' -Body (@{ role = 'candidate' } | ConvertTo-Json)
}

$candidateToken = $null
if ($candidateLogin) { $candidateToken = $candidateLogin.token }

if ($candidateToken) {
  $candidateHeaders = @{ Authorization = "Bearer $candidateToken" }

  Invoke-Api 'Auth Me (candidate token)' { Invoke-RestMethod -Uri "$base/auth/me" -Method Get -Headers $candidateHeaders } | Out-Null

  Invoke-Api 'Candidate cannot create job (expect 403)' {
    Invoke-RestMethod -Uri "$base/jobs" -Method Post -Headers $candidateHeaders -ContentType 'application/json' -Body (@{
      title = 'Should Fail'
      categoryId = ''
      description = 'x'
      responsibilities = @('x')
      mustHaveSkills = @('x')
      niceToHaveSkills = @()
      minExperienceYears = 0
    } | ConvertTo-Json)
  } | Out-Null

  Invoke-Api 'Candidate applications list' {
    Invoke-RestMethod -Uri "$base/applications/my" -Method Get -Headers $candidateHeaders
  } | Out-Null

  Invoke-Api 'AI suggest jobs' {
    Invoke-RestMethod -Uri "$base/ai/suggest-jobs" -Method Post -Headers $candidateHeaders -ContentType 'application/json' -Body (@{ cvText = 'C# developer with MongoDB and Vue experience' } | ConvertTo-Json)
  } | Out-Null

  $recruiterLogin2 = Invoke-Api 'Update Role -> recruiter' {
    Invoke-RestMethod -Uri "$base/auth/update-role" -Method Put -Headers $candidateHeaders -ContentType 'application/json' -Body (@{ role = 'recruiter' } | ConvertTo-Json)
  }

  if ($recruiterLogin2) {
    $recruiterToken = $recruiterLogin2.token
    $recruiterHeaders = @{ Authorization = "Bearer $recruiterToken" }
  }
}

if ($newJob -and $newJob.id) {
  Invoke-Api 'Recruiter applications by job' {
    Invoke-RestMethod -Uri "$base/applications/job/$($newJob.id)" -Method Get -Headers $recruiterHeaders
  } | Out-Null

  Invoke-Api 'Delete Job' {
    Invoke-RestMethod -Uri "$base/jobs/$($newJob.id)" -Method Delete -Headers $recruiterHeaders
  } | Out-Null
}

$results | ConvertTo-Json -Depth 6
