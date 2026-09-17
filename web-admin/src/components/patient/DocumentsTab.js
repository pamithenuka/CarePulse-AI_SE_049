import { useEffect, useRef, useState } from "react";
import { deleteDocument, fetchDocumentBlobUrl, updateDocument, uploadDocument } from "../../api/patientsApi";
import { EmptyState } from "../StatusView";
import "./PatientTabs.css";

const DOCUMENT_TYPES = ["Prescription", "LabReport", "ImagingScan", "Other"];

export default function DocumentsTab({ patientId, initialDocuments, isAdmin, onChanged }) {
  const [documents, setDocuments] = useState(initialDocuments);
  const [error, setError] = useState("");
  const [editingId, setEditingId] = useState(null);
  const [editForm, setEditForm] = useState({ fileName: "", documentType: "Other" });
  const [previewingId, setPreviewingId] = useState(null);
  const [uploadType, setUploadType] = useState("Other");
  const [isUploading, setIsUploading] = useState(false);
  const fileInputRef = useRef(null);

  useEffect(() => setDocuments(initialDocuments), [initialDocuments]);

  const handleUpload = async (event) => {
    event.preventDefault();
    const file = fileInputRef.current?.files?.[0];
    if (!file) {
      setError("Choose a file to upload.");
      return;
    }

    setError("");
    setIsUploading(true);
    try {
      const uploaded = await uploadDocument(patientId, file, uploadType);
      setDocuments((prev) => [uploaded, ...prev]);
      if (fileInputRef.current) fileInputRef.current.value = "";
      onChanged();
    } catch (err) {
      setError(err.response?.data?.message || "Failed to upload document.");
    } finally {
      setIsUploading(false);
    }
  };

  const handleView = async (doc, forceDownload) => {
    setError("");
    setPreviewingId(doc.id);
    try {
      const blobUrl = await fetchDocumentBlobUrl(patientId, doc.id);
      if (forceDownload) {
        const link = document.createElement("a");
        link.href = blobUrl;
        link.download = doc.fileName;
        link.click();
      } else {
        window.open(blobUrl, "_blank", "noopener,noreferrer");
      }
    } catch (err) {
      setError(err.response?.data?.message || "Failed to open document.");
    } finally {
      setPreviewingId(null);
    }
  };

  const startEdit = (doc) => {
    setEditingId(doc.id);
    setEditForm({ fileName: doc.fileName, documentType: doc.documentType });
  };

  const handleUpdate = async (event) => {
    event.preventDefault();
    setError("");
    try {
      const updated = await updateDocument(patientId, editingId, editForm);
      setDocuments((prev) => prev.map((d) => (d.id === editingId ? updated : d)));
      setEditingId(null);
      onChanged();
    } catch (err) {
      setError(err.response?.data?.message || "Failed to update document.");
    }
  };

  const handleDelete = async (documentId) => {
    if (!window.confirm("Remove this document?")) return;
    setError("");
    try {
      await deleteDocument(patientId, documentId);
      setDocuments((prev) => prev.filter((d) => d.id !== documentId));
      onChanged();
    } catch (err) {
      setError(err.response?.data?.message || "Failed to remove document.");
    }
  };

  return (
    <div className="detail-card">
      <h2>Medical Documents</h2>
      {error && (
        <div className="alert alert-error" role="alert">
          {error}
        </div>
      )}

      {documents.length === 0 ? (
        <EmptyState message="No documents uploaded yet." />
      ) : (
        <ul className="document-list">
          {documents.map((doc) =>
            editingId === doc.id ? (
              <li key={doc.id}>
                <form className="inline-edit-form" onSubmit={handleUpdate}>
                  <input
                    value={editForm.fileName}
                    onChange={(e) => setEditForm((f) => ({ ...f, fileName: e.target.value }))}
                  />
                  <select
                    value={editForm.documentType}
                    onChange={(e) => setEditForm((f) => ({ ...f, documentType: e.target.value }))}
                  >
                    {DOCUMENT_TYPES.map((type) => (
                      <option key={type} value={type}>
                        {type}
                      </option>
                    ))}
                  </select>
                  <div className="form-actions">
                    <button type="submit">Save</button>
                    <button type="button" className="btn-secondary" onClick={() => setEditingId(null)}>
                      Cancel
                    </button>
                  </div>
                </form>
              </li>
            ) : (
              <li key={doc.id}>
                <button type="button" className="link-button" onClick={() => handleView(doc, false)} disabled={previewingId === doc.id}>
                  {doc.fileName}
                </button>
                <span className="badge">{doc.documentType}</span>
                <div className="row-actions">
                  <button type="button" onClick={() => handleView(doc, true)} disabled={previewingId === doc.id}>
                    Download
                  </button>
                  {isAdmin && (
                    <>
                      <button type="button" onClick={() => startEdit(doc)}>
                        Rename / recategorize
                      </button>
                      <button type="button" className="btn-danger-link" onClick={() => handleDelete(doc.id)}>
                        Remove
                      </button>
                    </>
                  )}
                </div>
              </li>
            )
          )}
        </ul>
      )}

      {isAdmin && (
        <form className="inline-add-form" onSubmit={handleUpload}>
          <h3>Upload Document</h3>
          <div className="history-form-row">
            <input type="file" ref={fileInputRef} accept="image/*,application/pdf" />
            <select value={uploadType} onChange={(e) => setUploadType(e.target.value)}>
              {DOCUMENT_TYPES.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </div>
          <button type="submit" disabled={isUploading}>
            {isUploading ? "Uploading..." : "Upload"}
          </button>
        </form>
      )}
    </div>
  );
}
