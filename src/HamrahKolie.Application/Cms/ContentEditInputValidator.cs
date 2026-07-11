using FluentValidation;

namespace HamrahKolie.Application.Cms;

public class ContentEditInputValidator : AbstractValidator<ContentEditInput>
{
    public ContentEditInputValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("عنوان را وارد کنید.")
            .MaximumLength(300).WithMessage("عنوان نباید بیش از ۳۰۰ نویسه باشد.");

        RuleFor(x => x.Summary)
            .MaximumLength(1000).WithMessage("خلاصه نباید بیش از ۱۰۰۰ نویسه باشد.");

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500).WithMessage("توضیح متا نباید بیش از ۵۰۰ نویسه باشد.");

        RuleFor(x => x.Slug)
            .MaximumLength(320);
    }
}
