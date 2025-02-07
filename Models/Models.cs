using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    public string Address { get; set; }
    public bool IsFirstLogin { get; set; }
    public DateTime JoinDate { get; set; }
    public DateTime Expired { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpired { get; set; }
    public string? OTP { get; set; } // Lưu mã OTP
    public DateTime? OTPExpired { get; set; } // Lưu thời gian hết hạn của OTP
    public string? Imagepath { get; set; }
    public Student? Student { get; set; }
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
    public string Address { get; set; }
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
    public string Address { get; set; }
    public DateTime JoinDate { get; set; }
    public DateTime Dob { get; set; }

    // Các mối quan hệ sẽ là danh sách các đối tượng ID hoặc chi tiết
  
    public ICollection<int>? StaffSubjectIds { get; set; }
    public ICollection<int>? StaffQualificationIds { get; set; }
  
}



public class Staff
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime JoinDate { get; set; }

    // Có phải la nguoi co chuc nang insert/edit khong
    public bool IsReviewer { get; set; }
    public User User { get; set; }
    public ICollection<Class>? Classes { get; set; }
    public ICollection<StaffSubject>? StaffSubjects { get; set; }
    public ICollection<StaffQualification>? StaffQualifications { get; set; }
    public ICollection<Contest>? OrganizedContests { get; set; }
    public ICollection<Exhibition>? OrganizedExhibitions { get; set; }
    public ICollection<SubmissionReview>? SubmissionReviews { get; set; }
    public IEnumerable<ContestJudge>? ContestJudge { get; set; }
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int OrganizedBy { get; set; }
    // trang thai duyet cua cuoc thi

    [RegularExpression("^(Draft|Pending|Rejected|Approved|Published|Canceled)$")]
    public string Status { get; set; } = "Draft";
    //trang thai tien trinh 

    [RegularExpression("^(Upcoming|Ongoing|Completed)$")]
    public string Phase { get; set; } = "Upcoming";
    // UpComing, OnGoing, Completed
    public string CancelReason { get; set; } = "NULL";

    public string? Thumbnail { get; set; }
    public Staff? Organizer { get; set; }
    public ICollection<Award>? Awards { get; set; }
    public ICollection<Submission>? Submissions { get; set; }
    public ICollection<Condition>? Conditions {  get; set; } 

    public IEnumerable<ContestJudge>? ContestJudge { get; set; }
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
    public string? Status { get; set; }

    // Thêm thuộc tính lưu điểm trung bình
    public double? AverageRating { get; set; }

    public Student? Student { get; set; }
    public Contest? Contest { get; set; }
    public Artwork? Artwork { get; set; }


    public ICollection<SubmissionReview>? SubmissionReviews { get; set; }
}

public class Artwork
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public float Price { get; set; } = 100;

    [RegularExpression("^(Available|Sold)$")]
    public string? Status { get; set; } = "Available";
    public float? SellingPrice { get; set; }

    [RegularExpression("^(Unpaid|Paid)$")]
    public string? PaymentStatus { get; set; } = "Unpaid";
    
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
    public string Description { get; set; } 
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Location { get; set; }
    public int OrganizedBy { get; set; }
    public string? thumbnail {  get; set; }

    //trang thai duyet cua trien lam
    [RegularExpression("^(Draft|Pending|Rejected|Approved|Published|Canceled)$")]
   
    public string Status { get; set; } = "Draft";

    //trang thai tien trinh 
    [RegularExpression("^(Upcoming|Ongoing|Completed)$")]
    public string Phase { get; set; } = "Upcoming";
    // UpComing, OnGoing, Completed

    public string CancelReason { get; set; } = "NULL";


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

    [RegularExpression("^(Draft|Pending|Rejected|Approved|Published|Canceled)$", ErrorMessage = "Invalid Value")]
    public string Status { get; set; } = "Draft";
    public bool IsAwarded { get; set; }

    public Contest? Contest { get; set; }
    public ICollection<StudentAward>? StudentAwards { get; set; }
}

public class StudentAward
{
    public int StudentId { get; set; }
    public int AwardId { get; set; }

    [RegularExpression("^(Draft|Pending|Rejected|Approved|Published|Canceled)$")]
    public string Status { get; set; } = "Draft"; 

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

public class Condition
{
    public int Id { get; set; }
    public int ContestId { get; set; }
    public string Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdateAt {  get; set; } = DateTime.Now;

    public Contest? Contest { get; set; }
}


public enum ReferenceType { Contest, Award, Exhibition, ArtWork};/**/
public class Reject
{
    public int Id { get; set; }

    public int ReferenceId { get; set; }

    [RegularExpression("^(Contest|Award|Exhibition|ArtWork)$")]
    public string RefrenceType { get; set; }
    public int RejectedBy { get; set; }
    [RegularExpression("")]
    public string RejectReason { get; set; }    

    public DateTime RejectDate { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

}

public class Request
{
    public int Id { get; set; }
    public DateTime MeetingTime { get; set; }
    [RegularExpression("^(Canceled|Preparing|Done)$")]
    public string Status { get; set; }
    public string Description { get; set; }
    public string Organized { get; set; }

}

public class ContestJudge
{
    public int StaffId { get; set; }
    public int ContestId { get; set;}

    [RegularExpression("^(Draft|Pending|Rejected|Approved|Published|Canceled)$")]
    public string status { get; set; } = "Draft";
    public Contest? Contest { get; set; }

    public Staff? Staff { get; set; }
}

