#ifndef  VertexFormat_H_
#define VertexFormat_H_

#include "glm\glm.hpp" //installed with NuGet

struct VertexFormat
{
	glm::vec3 position;//our first vertex attribute
	glm::vec4 color;

	VertexFormat(const glm::vec3 &aPos, const glm::vec4 &aColor)
	{
		position = aPos;
		color = aColor;
	}
};

#endif