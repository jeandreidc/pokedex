group "default" {
  targets = ["api", "web"]
}

variable "TAG" {
  default = "latest"
}

target "api" {
  context    = "."
  dockerfile = "infra/docker/Dockerfile"
  target     = "api"
  tags       = ["kota-pokedex-api:${TAG}"]
}

target "web" {
  context    = "."
  dockerfile = "infra/docker/Dockerfile"
  target     = "web"
  tags       = ["kota-pokedex-web:${TAG}"]
}
