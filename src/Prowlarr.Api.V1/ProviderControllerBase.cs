using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;
using NzbDrone.Http.REST.Attributes;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1
{
    public abstract class ProviderControllerBase<TProviderResource, TProvider, TProviderDefinition> : RestController<TProviderResource>
        where TProviderDefinition : ProviderDefinition, new()
        where TProvider : IProvider
        where TProviderResource : ProviderResource<TProviderResource>, new()
    {
        protected readonly IProviderFactory<TProvider, TProviderDefinition> _providerFactory;
        protected readonly ProviderResourceMapper<TProviderResource, TProviderDefinition> _resourceMapper;

        protected ProviderControllerBase(IProviderFactory<TProvider, TProviderDefinition> providerFactory, string resource, ProviderResourceMapper<TProviderResource, TProviderDefinition> resourceMapper)
        {
            _providerFactory = providerFactory;
            _resourceMapper = resourceMapper;

            SharedValidator.RuleFor(c => c.Name).NotEmpty();
            SharedValidator.RuleFor(c => c.Name).Must((v, c) => !_providerFactory.All().Any(p => p.Name == c && p.Id != v.Id)).WithMessage("Should be unique");
            SharedValidator.RuleFor(c => c.Implementation).NotEmpty();
            SharedValidator.RuleFor(c => c.ConfigContract).NotEmpty();

            PostValidator.RuleFor(c => c.Fields).NotNull();
        }

        public override TProviderResource GetResourceById(int id)
        {
            var definition = _providerFactory.Get(id);
            _providerFactory.SetProviderCharacteristics(definition);

            return _resourceMapper.ToResource(definition);
        }

        [HttpGet]
        [Produces("application/json")]
        public List<TProviderResource> GetAll()
        {
            var providerDefinitions = _providerFactory.All();

            var result = new List<TProviderResource>(providerDefinitions.Count);

            foreach (var definition in providerDefinitions.OrderBy(p => p.ImplementationName))
            {
                _providerFactory.SetProviderCharacteristics(definition);

                result.Add(_resourceMapper.ToResource(definition));
            }

            return result.OrderBy(p => p.Name).ToList();
        }

        [RestPostById]
        [Consumes("application/json")]
        [Produces("application/json")]
        public ActionResult<TProviderResource> CreateProvider([FromBody] TProviderResource providerResource, [FromQuery] bool forceSave = false)
        {
            var providerDefinition = GetDefinition(providerResource, true, !forceSave, false);

            if (providerDefinition.Enable)
            {
                Test(providerDefinition, false);
            }

            providerDefinition = _providerFactory.Create(providerDefinition);

            return Created(providerDefinition.Id);
        }

        [RestPutById]
        [Consumes("application/json")]
        [Produces("application/json")]
        public ActionResult<TProviderResource> UpdateProvider([FromBody] TProviderResource providerResource, [FromQuery] bool forceSave = false)
        {
            var providerDefinition = GetDefinition(providerResource, true, !forceSave, false);

            // Only test existing definitions if it is enabled and forceSave isn't set.
            if (providerDefinition.Enable && !forceSave)
            {
                Test(providerDefinition, false);
            }

            _providerFactory.Update(providerDefinition);

            return Accepted(providerResource.Id);
        }

        private TProviderDefinition GetDefinition(TProviderResource providerResource, bool validate, bool includeWarnings, bool forceValidate)
        {
            var definition = _resourceMapper.ToModel(providerResource);

            if (validate && (definition.Enable || forceValidate))
            {
                Validate(definition, includeWarnings);
            }

            return definition;
        }

        [RestDeleteById]
        public object DeleteProvider(int id)
        {
            _providerFactory.Delete(id);

            return new { };
        }

        [HttpGet("schema")]
        [Produces("application/json")]
        public virtual List<TProviderResource> GetTemplates()
        {
            var defaultDefinitions = _providerFactory.GetDefaultDefinitions().OrderBy(p => p.ImplementationName).ToList();

            var result = new List<TProviderResource>(defaultDefinitions.Count);

            foreach (var providerDefinition in defaultDefinitions)
            {
                var providerResource = _resourceMapper.ToResource(providerDefinition);
                var presetDefinitions = _providerFactory.GetPresetDefinitions(providerDefinition);

                providerResource.Presets = presetDefinitions
                    .Select(v => _resourceMapper.ToResource(v))
                    .ToList();

                result.Add(providerResource);
            }

            return result;
        }

        [SkipValidation(true, false)]
        [HttpPost("test")]
        [Consumes("application/json")]
        public object Test([FromBody] TProviderResource providerResource)
        {
            var providerDefinition = GetDefinition(providerResource, true, true, true);

            Test(providerDefinition, true);

            return "{}";
        }

        [HttpPost("testall")]
        public IActionResult TestAll()
        {
            var providerDefinitions = _providerFactory.All()
                                                      .Where(c => c.Settings.Validate().IsValid && c.Enable)
                                                      .ToList();
            var result = new List<ProviderTestAllResult>();

            foreach (var definition in providerDefinitions)
            {
                var validationResult = _providerFactory.Test(definition);

                result.Add(new ProviderTestAllResult
                {
                    Id = definition.Id,
                    ValidationFailures = validationResult.Errors.ToList()
                });
            }

            return result.Any(c => !c.IsValid) ? BadRequest(result) : Ok(result);
        }

        [SkipValidation]
        [HttpPost("action/{name}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public IActionResult RequestAction(string name, [FromBody] TProviderResource resource)
        {
            var providerDefinition = GetDefinition(resource, false, false, false);

            var query = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());

            var data = _providerFactory.RequestAction(providerDefinition, name, query);

            return Json(data);
        }

        private void Validate(TProviderDefinition definition, bool includeWarnings)
        {
            var validationResult = definition.Settings.Validate();

            VerifyValidationResult(validationResult, includeWarnings);
        }

        protected virtual void Test(TProviderDefinition definition, bool includeWarnings)
        {
            var validationResult = _providerFactory.Test(definition);

            VerifyValidationResult(validationResult, includeWarnings);
        }

        protected void VerifyValidationResult(ValidationResult validationResult, bool includeWarnings)
        {
            var result = validationResult as NzbDroneValidationResult ?? new NzbDroneValidationResult(validationResult.Errors);

            if (includeWarnings && (!result.IsValid || result.HasWarnings))
            {
                throw new ValidationException(result.Failures);
            }

            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }
    }
}
