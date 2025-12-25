namespace DynamicAllowListingLib
{
  public interface ISettingValidator<T>
  {
    public ResultObject Validate(T settings);
    public ResultObject ValidateFormat(T settings);
  }
}