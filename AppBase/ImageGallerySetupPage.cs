using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class GalleryImageRecord
    {
        public int GalleryImageID { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public string OriginalFileName { get; set; } = "";
        public int SortOrder { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public string PreviewUrl => GalleryImagePage.UrlFor(GalleryImageID);
    }

    public class ImageGallerySetupPage : AppBasePage
    {
        private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".webp", ".gif" };

        public string PageTitle => "Image Gallery Setup";
        public GalleryImageRecord Input { get; private set; } = new GalleryImageRecord { IsActive = true, SortOrder = 1 };
        public List<GalleryImageRecord> Records { get; private set; } = new List<GalleryImageRecord>();
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Form != null)
                Form.Enctype = "multipart/form-data";

            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? "Save";
                if (string.Equals(handler, "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    OnPostDelete();
                    return;
                }
                OnPostSave();
                return;
            }

            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            var editId = QueryInt("editId");
            if (editId.HasValue && editId.Value > 0)
                LoadForEdit(editId.Value);
            LoadRecords();
        }

        private void OnPostSave()
        {
            if (!Auth.IsAdmin && !Perms.CanWrite("ImageGallerySetup"))
            {
                SetAlert(PermissionService.AccessRestrictedMessage, "error");
                Response.Redirect("~/ImageGallerySetup.aspx");
                return;
            }

            var id = FormInt("galleryImageID");
            var title = FormString("title");
            var description = FormString("description");
            var sortOrder = FormInt("sortOrder");
            var isActive = FormBool("isActive");
            if (sortOrder < 0) sortOrder = 0;

            if (string.IsNullOrWhiteSpace(title))
            {
                SetAlert("Title is required.", "error");
                Response.Redirect("~/ImageGallerySetup.aspx");
                return;
            }

            try
            {
                string imagePath = null;
                string originalName = null;
                if (id > 0)
                {
                    var existing = LoadRecordById(id);
                    if (existing != null)
                    {
                        imagePath = existing.ImagePath;
                        originalName = existing.OriginalFileName;
                    }
                }

                var imageFile = Request.Files["imageFile"];
                var hasNewFile = imageFile != null && imageFile.ContentLength > 0;
                if (hasNewFile && id > 0)
                {
                    var saved = SaveImageFile(imageFile, id);
                    imagePath = saved.Item1;
                    originalName = saved.Item2;
                }

                if (id <= 0 && !hasNewFile && string.IsNullOrWhiteSpace(imagePath))
                {
                    SetAlert("Please upload an image.", "error");
                    Response.Redirect("~/ImageGallerySetup.aspx");
                    return;
                }

                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    if (id > 0)
                    {
                        using (var cmd = new SqlCommand(@"
UPDATE tblGalleryImage SET
    Title = @Title, Description = @Description, ImagePath = @ImagePath,
    OriginalFileName = @OriginalFileName, SortOrder = @SortOrder, IsActive = @IsActive,
    ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
WHERE GalleryImageID = @Id;", conn))
                        {
                            BindParams(cmd, id, title, description, imagePath, originalName, sortOrder, isActive);
                            AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Gallery image updated successfully.");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(@"
INSERT INTO tblGalleryImage
    (Title, Description, ImagePath, OriginalFileName, SortOrder, IsActive, CreatedOn, CreatedByUserID)
VALUES
    (@Title, @Description, @ImagePath, @OriginalFileName, @SortOrder, @IsActive, GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
                        {
                            BindParams(cmd, 0, title, description, imagePath, originalName, sortOrder, isActive, true);
                            AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            var newId = Convert.ToInt32(cmd.ExecuteScalar());
                            if (hasNewFile && newId > 0)
                            {
                                var saved = SaveImageFile(imageFile, newId);
                                using (var upd = new SqlCommand(
                                    "UPDATE tblGalleryImage SET ImagePath=@Path, OriginalFileName=@Orig WHERE GalleryImageID=@Id;", conn))
                                {
                                    upd.Parameters.AddWithValue("@Path", saved.Item1);
                                    upd.Parameters.AddWithValue("@Orig", saved.Item2);
                                    upd.Parameters.AddWithValue("@Id", newId);
                                    upd.ExecuteNonQuery();
                                }
                            }
                        }
                        SetAlert("Gallery image added successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }

            Response.Redirect("~/ImageGallerySetup.aspx");
        }

        private void OnPostDelete()
        {
            if (!Auth.IsAdmin && !Perms.CanWrite("ImageGallerySetup") && !Perms.CanDelete("ImageGallerySetup"))
            {
                SetAlert(PermissionService.AccessRestrictedMessage, "error");
                Response.Redirect("~/ImageGallerySetup.aspx");
                return;
            }

            var deleteId = FormInt("deleteId");
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
UPDATE tblGalleryImage SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
WHERE GalleryImageID = @Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", deleteId);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Gallery image deactivated successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }
            Response.Redirect("~/ImageGallerySetup.aspx");
        }

        private void LoadForEdit(int id)
        {
            var rec = LoadRecordById(id);
            if (rec != null) Input = rec;
        }

        private GalleryImageRecord LoadRecordById(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT GalleryImageID, Title, Description, ImagePath, OriginalFileName, SortOrder, IsActive
FROM tblGalleryImage WHERE GalleryImageID = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    return Map(dr);
                }
            }
        }

        private void LoadRecords()
        {
            Records.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT GalleryImageID, Title, Description, ImagePath, OriginalFileName, SortOrder, IsActive
FROM tblGalleryImage
ORDER BY IsActive DESC, SortOrder, GalleryImageID;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        Records.Add(Map(dr));
                }
            }
        }

        private static void BindParams(SqlCommand cmd, int id, string title, string description,
            string imagePath, string originalName, int sortOrder, bool isActive, bool isInsert = false)
        {
            if (!isInsert) cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Title", title.Trim());
            cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description.Trim());
            cmd.Parameters.AddWithValue("@ImagePath", string.IsNullOrWhiteSpace(imagePath) ? (object)DBNull.Value : imagePath);
            cmd.Parameters.AddWithValue("@OriginalFileName", string.IsNullOrWhiteSpace(originalName) ? (object)DBNull.Value : originalName);
            cmd.Parameters.AddWithValue("@SortOrder", sortOrder);
            cmd.Parameters.AddWithValue("@IsActive", isActive);
        }

        private Tuple<string, string> SaveImageFile(HttpPostedFile file, int galleryImageId)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException("File type not allowed. Use PNG, JPG, JPEG, WEBP, or GIF.");

            var uploads = Server.MapPath("~/uploads/gallery");
            Directory.CreateDirectory(uploads);
            var safeName = "gallery_" + galleryImageId + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ext;
            file.SaveAs(Path.Combine(uploads, safeName));
            return Tuple.Create("/uploads/gallery/" + safeName, Path.GetFileName(file.FileName));
        }

        private static GalleryImageRecord Map(SqlDataReader dr)
        {
            return new GalleryImageRecord
            {
                GalleryImageID = Convert.ToInt32(dr["GalleryImageID"]),
                Title = dr["Title"].ToString() ?? "",
                Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
                ImagePath = dr["ImagePath"] == DBNull.Value ? "" : dr["ImagePath"].ToString() ?? "",
                OriginalFileName = dr["OriginalFileName"] == DBNull.Value ? "" : dr["OriginalFileName"].ToString() ?? "",
                SortOrder = dr["SortOrder"] == DBNull.Value ? 1 : Convert.ToInt32(dr["SortOrder"]),
                IsActive = Convert.ToBoolean(dr["IsActive"])
            };
        }
    }
}
