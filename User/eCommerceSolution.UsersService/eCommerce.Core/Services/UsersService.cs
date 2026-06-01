using AutoMapper;
using eCommerce.Core.DTO;
using eCommerce.Core.Entities;
using eCommerce.Core.Exceptions;
using eCommerce.Core.RepositoryContracts;
using eCommerce.Core.ServiceContracts;
using FluentValidation;
using FluentValidation.Results;

namespace eCommerce.Core.Services
{
    internal class UsersService : IUsersService
    {
        private readonly IUsersRepository _usersRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<LoginRequest> _loginValidator;
        private readonly IValidator<RegisterRequest> _registerValidator;

        public UsersService(
            IUsersRepository usersRepository,
            IMapper mapper,
            IValidator<LoginRequest> loginValidator,
            IValidator<RegisterRequest> registerValidator)
        {
            _usersRepository = usersRepository;
            _mapper = mapper;
            _loginValidator = loginValidator;
            _registerValidator = registerValidator;
        }

        public async Task<AuthenticationResponse> Login(LoginRequest loginRequest)
        {
            await ValidateAsync(_loginValidator, loginRequest);

            ApplicationUser? user = await _usersRepository.GetUserByEmailAndPassword(loginRequest.Email, loginRequest.Password);

            if (user == null)
            {
                throw new InvalidCredentialsException();
            }

            return _mapper.Map<AuthenticationResponse>(user) with { Success = true, Token = "token" };
        }

        public async Task<AuthenticationResponse> Register(RegisterRequest registerRequest)
        {
            await ValidateAsync(_registerValidator, registerRequest);

            ApplicationUser? existingUser = await _usersRepository.GetUserByEmail(registerRequest.Email!);

            if (existingUser != null)
            {
                throw new ConflictException("Email já cadastrado.");
            }

            ApplicationUser user = _mapper.Map<ApplicationUser>(registerRequest);
            ApplicationUser registeredUser = await _usersRepository.AddUser(user);

            return _mapper.Map<AuthenticationResponse>(registeredUser) with { Success = true, Token = "token" };
        }

        private static async Task ValidateAsync<T>(IValidator<T> validator, T instance)
        {
            ValidationResult validationResult = await validator.ValidateAsync(instance);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }
    }
}
