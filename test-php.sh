docker run --rm -v $PWD/build/:/app -w /app php:7.4-cli php vendor/phpunit/phpunit/phpunit Fable.Tests.Php/$@