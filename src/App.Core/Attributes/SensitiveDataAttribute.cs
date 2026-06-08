namespace App.Core.Attributes;

/// <summary>
/// Marks an entity property whose value must NOT be written verbatim to the audit log.
/// The AuditLogInterceptor still records that the property changed, but replaces the
/// old/new values with a redacted placeholder so secrets (passwords, tokens, private
/// keys) never land in <c>aud_change_log</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SensitiveDataAttribute : Attribute
{
}
