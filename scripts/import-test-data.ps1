# Test Data Import Script
# This script populates MongoDB with sample data for testing

$API_BASE = "http://localhost:5187/api"

# Color output
function Log-Success { Write-Host "[OK] $args" -ForegroundColor Green }
function Log-Error { Write-Host "[XX] $args" -ForegroundColor Red }
function Log-Info { Write-Host "[>>] $args" -ForegroundColor Cyan }

# Test user credentials (from previous smoke test)
$RECRUITER_EMAIL = "hntvnvn123@gmail.com"
$RECRUITER_PASSWORD = "Google123"

Log-Info "Starting Test Data Import..."

try {
    # Step 1: Get or create categories
    Log-Info "Checking categories..."
    $categoriesResponse = Invoke-RestMethod -Uri "$API_BASE/categories" -Method Get
    $categories = $categoriesResponse
    
    if ($categories.Length -eq 0) {
        Log-Info "No categories found, creating sample categories..."
        
        # First, authenticate to create categories (requires auth)
        Log-Info "Authenticating as recruiter for category creation..."
        $authBody = @{
            email = $RECRUITER_EMAIL
            password = $RECRUITER_PASSWORD
        } | ConvertTo-Json

        try {
            $authResponse = Invoke-RestMethod -Uri "$API_BASE/auth/login" -Method Post -Body $authBody -ContentType "application/json"
            $token = $authResponse.token
            $tempHeaders = @{
                "Authorization" = "Bearer $token"
                "Content-Type" = "application/json"
            }
        } catch {
            Log-Error "Authentication failed: $($_.Exception.Response.StatusCode)"
            exit 1
        }
        
        $sampleCategories = @(
            @{ name = "Software Development"; slug = "software-development" },
            @{ name = "Data Science"; slug = "data-science" },
            @{ name = "DevOps & Infrastructure"; slug = "devops-infrastructure" },
            @{ name = "Mobile Development"; slug = "mobile-development" },
            @{ name = "Full Stack"; slug = "full-stack" },
            @{ name = "UI/UX Design"; slug = "uiux-design" },
            @{ name = "Business & Management"; slug = "business-management" },
            @{ name = "Marketing"; slug = "marketing" }
        )
        
        foreach ($cat in $sampleCategories) {
            try {
                $catBody = $cat | ConvertTo-Json
                $result = Invoke-RestMethod -Uri "$API_BASE/categories" -Method Post -Headers $tempHeaders -Body $catBody
                Log-Success "Created category: $($cat.name)"
            } catch {
                Log-Error "Failed to create category: $($_.Exception.Response.StatusCode)"
            }
        }
        
        # Fetch categories again
        $categoriesResponse = Invoke-RestMethod -Uri "$API_BASE/categories" -Method Get
        $categories = $categoriesResponse
    }
    
    Log-Success "Found $($categories.Length) categories"
    $categories | ForEach-Object { Log-Info "  - $($_.name)" }

    # Step 2: Authenticate as recruiter
    Log-Info "Authenticating as recruiter..."
    $authBody = @{
        email = $RECRUITER_EMAIL
        password = $RECRUITER_PASSWORD
    } | ConvertTo-Json

    try {
        $authResponse = Invoke-RestMethod -Uri "$API_BASE/auth/login" -Method Post -Body $authBody -ContentType "application/json"
        $token = $authResponse.token
        $userId = $authResponse.userId
        Log-Success "Authenticated as: $($authResponse.email) (Role: $($authResponse.role))"
    } catch {
        Log-Error "Authentication failed: $($_.Exception.Response.StatusCode)"
        exit 1
    }

    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }

    # Step 3: Create test companies
    Log-Info "Creating test companies..."
    
    $companies = @(
        @{
            companyName = "TechVenture Solutions"
            description = "Leading software development company specializing in cloud solutions"
            industry = "Technology"
            size = "100-500"
            website = "https://techventure.example.com"
            headquarters = "Ho Chi Minh City"
        },
        @{
            companyName = "Digital Innovation Labs"
            description = "AI and machine learning solutions provider"
            industry = "AI/ML"
            size = "50-100"
            website = "https://diginnovate.example.com"
            headquarters = "Hanoi"
        },
        @{
            companyName = "E-Commerce Masters"
            description = "Largest e-commerce platform in Southeast Asia"
            industry = "E-Commerce"
            size = "500+"
            website = "https://ecomasters.example.com"
            headquarters = "Singapore"
        }
    )

    $companyIds = @()
    foreach ($company in $companies) {
        try {
            $body = $company | ConvertTo-Json
            $response = Invoke-RestMethod -Uri "$API_BASE/companies" -Method Post -Headers $headers -Body $body
            $companyIds += $response._id
            Log-Success "Created company: $($company.companyName)"
        } catch {
            Log-Error "Failed to create company '$($company.companyName)': $($_.Exception.Response.StatusCode)"
        }
    }

    # Step 4: Create test jobs
    Log-Info "Creating test jobs for different companies..."
    
    $jobTitles = @(
        @{
            title = "Senior Backend Engineer"
            description = "We're looking for an experienced backend engineer to build scalable APIs"
            requirements = @("5+ years experience", ".NET or Node.js", "MongoDB", "Docker", "CI/CD")
            salary = "15-20 million VND/month"
            experience = "Senior"
            skillsRequired = @("C#", "SQL", "AWS")
        },
        @{
            title = "Frontend Developer (React)"
            description = "Join our team to build modern web interfaces"
            requirements = @("3+ years React", "TypeScript", "Tailwind CSS", "Testing")
            salary = "12-16 million VND/month"
            experience = "Mid-level"
            skillsRequired = @("React", "JavaScript", "CSS")
        },
        @{
            title = "DevOps Engineer"
            description = "Manage and optimize our cloud infrastructure"
            requirements = @("5+ years DevOps", "Kubernetes", "CI/CD", "Linux")
            salary = "16-22 million VND/month"
            experience = "Senior"
            skillsRequired = @("Kubernetes", "Docker", "AWS")
        },
        @{
            title = "Data Science Engineer"
            description = "Build ML models for product recommendations"
            requirements = @("3+ years ML", "Python", "TensorFlow", "SQL")
            salary = "14-19 million VND/month"
            experience = "Mid-level"
            skillsRequired = @("Python", "Machine Learning", "SQL")
        },
        @{
            title = "Full Stack Developer"
            description = "Build end-to-end features for our platform"
            requirements = @("4+ years", "Vue/React", ".NET/Node.js", "MongoDB")
            salary = "13-18 million VND/month"
            experience = "Mid-level"
            skillsRequired = @("Vue", "JavaScript", ".NET")
        },
        @{
            title = "Mobile App Developer (Flutter)"
            description = "Develop cross-platform mobile apps"
            requirements = @("2+ years Flutter", "Dart", "Firebase", "UI/UX")
            salary = "11-15 million VND/month"
            experience = "Junior"
            skillsRequired = @("Flutter", "Dart", "Firebase")
        }
    )

    $jobIds = @()
    $jobIndex = 0
    
    foreach ($companyId in $companyIds) {
        for ($i = 0; $i -lt 2; $i++) {
            $jobIndex = ($jobIndex + 1) % $jobTitles.Length
            $jobData = $jobTitles[$jobIndex]
            
            $jobBody = @{
                jobTitle = $jobData.title
                description = $jobData.description
                requirements = $jobData.requirements
                salaryRange = $jobData.salary
                experienceLevel = $jobData.experience
                companyId = $companyId
                categoryId = $categories[($jobIndex % $categories.Length)]._id
                skillsRequired = $jobData.skillsRequired
                location = "Vietnam"
                jobType = if ($i -eq 0) { "Full-time" } else { "Contract" }
            } | ConvertTo-Json
            
            try {
                $response = Invoke-RestMethod -Uri "$API_BASE/jobs" -Method Post -Headers $headers -Body $jobBody
                $jobIds += $response._id
                $companyShort = $companyId.Substring(0, 8)
                Log-Success "Created job: $($jobData.title) for company ID: $companyShort..."
            } catch {
                Log-Error "Failed to create job: $($_.Exception.Response.StatusCode)"
            }
        }
    }

    # Step 5: Switch to candidate and create applications
    Log-Info "Switching to candidate role..."
    try {
        $roleBody = @{ role = "candidate" } | ConvertTo-Json
        $roleResponse = Invoke-RestMethod -Uri "$API_BASE/auth/switch-role" -Method Patch -Headers $headers -Body $roleBody
        $token = $roleResponse.token
        $headers["Authorization"] = "Bearer $token"
        Log-Success "Switched to candidate role"
    } catch {
        Log-Error "Failed to switch role: $($_.Exception.Response.StatusCode)"
    }

    # Create applications
    Log-Info "Creating test applications..."
    if ($jobIds.Length -gt 0) {
        foreach ($jobId in $jobIds | Select-Object -First 3) {
            try {
                $appBody = @{
                    jobId = $jobId
                    coverLetter = "I am applying for this position because I am passionate about this role and believe my skills align well with your requirements."
                } | ConvertTo-Json
                
                $response = Invoke-RestMethod -Uri "$API_BASE/applications" -Method Post -Headers $headers -Body $appBody
                $jobShort = $jobId.Substring(0, 8)
                Log-Success "Created application for job ID: $jobShort..."
            } catch {
                Log-Error "Failed to create application: $($_.Exception.Response.StatusCode)"
            }
        }
    }

    Log-Info ""
    Log-Success "Test data import completed!"
    Log-Info "Summary:"
    Log-Info "  - Categories: $($categories.Length)"
    Log-Info "  - Companies: $($companyIds.Length)"
    Log-Info "  - Jobs: $($jobIds.Length)"
    Log-Info "  - Applications: 3"
    Log-Info ""
    Log-Info "You can now:"
    Log-Info "  1. Login with your Google account"
    Log-Info "  2. Switch to recruiter role to see companies & jobs"
    Log-Info "  3. Switch to candidate role to see applications"

} catch {
    Log-Error "Unexpected error: $_"
    exit 1
}
