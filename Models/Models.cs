using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }
    public string Dob { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public bool Status { get; set; }
    public DateTime JoinDate { get; set; }
    public DateTime Expired { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpired { get; set; }
    public string? Imagepath { get; set; }
    public Student Student { get; set; }
    public Staff? Staff { get; set; }


}
public class CreateUserDto
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }
    public string Dob { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
}


public class UpdateUserDto
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Role { get; set; }
    public string? Name { get; set; }
    public string? Dob { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool? Status { get; set; }
    public string? Imagepath { get; set; }
}
public class UserAuth
{
    public string Email { get; set; }
    public string Password { get; set; }
}
public class Student
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public string ParentName { get; set; }
    public string ParentPhoneNumber { get; set; }

    public User? User { get; set; }
    public ICollection<StudentClass>? StudentClasses { get; set; }
    public ICollection<Submission>? Submissions { get; set; }
    public ICollection<StudentAward>? StudentAwards { get; set; }
}
public class CreateStudentRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public DateTime Dob { get; set; }
    public DateTime JoinDate { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public string ParentName { get; set; }
    public string ParentPhoneNumber { get; set; }
    public ICollection<int>? ClassIds { get; set; }
    public ICollection<int>? SubmissionIds { get; set; }
    public ICollection<int>? AwardIds { get; set; }
}

public class CreateStaffRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public DateTime JoinDate { get; set; }
    public DateTime Dob { get; set; }

    // Các mối quan hệ sẽ là danh sách các đối tượng ID hoặc chi tiết
    public ICollection<int>? ClassIds { get; set; }
    public ICollection<int>? StaffSubjectIds { get; set; }
    public ICollection<int>? StaffQualificationIds { get; set; }
    public ICollection<int>? ContestIds { get; set; }
    public ICollection<int>? ExhibitionIds { get; set; }
    public ICollection<int>? SubmissionReviewIds { get; set; }
}



public class Staff
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime JoinDate { get; set; }

    public User User { get; set; }
    public ICollection<Class>? Classes { get; set; }
    public ICollection<StaffSubject>? StaffSubjects { get; set; }
    public ICollection<StaffQualification>? StaffQualifications { get; set; }
    public ICollection<Contest>? OrganizedContests { get; set; }
    public ICollection<Exhibition>? OrganizedExhibitions { get; set; }
    public ICollection<SubmissionReview>? SubmissionReviews { get; set; }
}

public class Qualification
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime DateOfIssuance { get; set; }

    public ICollection<StaffQualification>? StaffQualifications { get; set; }
}

public class StaffQualification
{
    public int StaffId { get; set; }
    public int QualificationId { get; set; }

    public Staff? Staff { get; set; }
    public Qualification? Qualification { get; set; }
}

public class Class
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Year { get; set; }
    public int TotalStudent { get; set; }
    public int StaffId { get; set; }

    public Staff? Staff { get; set; }
    public ICollection<StudentClass>? StudentClasses { get; set; }
}

public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; }

    public ICollection<StaffSubject>? StaffSubjects { get; set; }
}

public class StaffSubject
{
    public int StaffId { get; set; }
    public int SubjectId { get; set; }

    public Staff? Staff { get; set; }
    public Subject? Subject { get; set; }
}

public class StudentClass
{
    public int StudentId { get; set; }
    public int ClassId { get; set; }

    public Student? Student { get; set; }
    public Class? Class { get; set; }
}

public class Contest
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime SubmissionDeadline { get; set; }
    public string ParticipationCriteria { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int OrganizedBy { get; set; }
    public bool IsActive { get; set; }

    public Staff? Organizer { get; set; }
    public ICollection<Award>? Awards { get; set; }
    public ICollection<Submission>? Submissions { get; set; }
}

public class Submission
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int ContestId { get; set; }
    public DateTime SubmissionDate { get; set; }
    public string Description { get; set; }
    public string Name { get; set; }
    public string FilePath { get; set; }

    public Student? Student { get; set; }
    public Contest? Contest { get; set; }
    public Artwork? Artwork { get; set; }
    public ICollection<SubmissionReview>? SubmissionReviews { get; set; }
}

public class Artwork
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public float Price { get; set; }
    public string? Status { get; set; }
    public float SellingPrice { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime ExhibitionDate { get; set; }

    public Submission? Submission { get; set; }
    public ICollection<ExhibitionArtwork>? ExhibitionArtworks { get; set; }
}

public class SubmissionReview
{
    public int SubmissionId { get; set; }
    public int StaffId { get; set; }
    public int RatingId { get; set; }
    public DateTime ReviewDate { get; set; }
    public string ReviewText { get; set; }

    public Submission? Submission { get; set; }
    public Staff? Staff { get; set; }
    public RatingLevel? RatingLevel { get; set; }
}

public class Exhibition
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Location { get; set; }
    public int OrganizedBy { get; set; }

    public Staff? Organizer { get; set; }
    public ICollection<ExhibitionArtwork>? ExhibitionArtworks { get; set; }
}


public class ExhibitionArtwork
{
    public int ExhibitionId { get; set; }
    public int ArtworkId { get; set; }

    public Exhibition? Exhibition { get; set; }
    public Artwork? Artwork { get; set; }
}

public class Award
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Value { get; set; }
    public int ContestId { get; set; }
    public int AwardQuantity { get; set; }

    public Contest? Contest { get; set; }
    public ICollection<StudentAward>? StudentAwards { get; set; }
}

public class StudentAward
{
    public int StudentId { get; set; }
    public int AwardId { get; set; }

    public Student? Student { get; set; }
    public Award? Award { get; set; }
}

public class RatingLevel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Mark { get; set; }
    public ICollection<SubmissionReview>? SubmissionReviews { get; set; }
}