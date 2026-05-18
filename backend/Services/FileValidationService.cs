namespace BreakThroughCV.API.Services;

public static class FileValidationService
{
    public static bool IsValidImage(IFormFile file, out string error)
    {
        error = string.Empty;
        if (file.Length == 0)
        {
            error = "File is empty";
            return false;
        }

        const long maxSize = 5 * 1024 * 1024;
        if (file.Length > maxSize)
        {
            error = "Image size must not exceed 5MB";
            return false;
        }

        var allowedTypes = new HashSet<string>
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        if (!allowedTypes.Contains(file.ContentType))
        {
            error = "Only JPEG, PNG, WEBP images are allowed";
            return false;
        }

        return true;
    }

    public static bool IsValidCv(IFormFile file, out string error)
    {
        error = string.Empty;
        if (file.Length == 0)
        {
            error = "File is empty";
            return false;
        }

        const long maxSize = 10 * 1024 * 1024;
        if (file.Length > maxSize)
        {
            error = "CV file size must not exceed 10MB";
            return false;
        }

        var allowedTypes = new HashSet<string>
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        if (!allowedTypes.Contains(file.ContentType))
        {
            error = "Only PDF, DOC, DOCX files are allowed";
            return false;
        }

        return true;
    }
}
